using System.Text.Json;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InspectionProcessor.Services;

namespace InspectionProcessor.Functions;

public class InspectionProcessorFunction
{
    private readonly ILogger _logger;
    private readonly string _blobConnectionString;
    private readonly string _containerName;
    private readonly string _emailConnectionString;
    private readonly string _emailFrom;
    private readonly string _emailTo;
    private readonly IInspectionApiClient _inspectionApiClient;

    public InspectionProcessorFunction(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IInspectionApiClient inspectionApiClient)
    {
        _logger = loggerFactory.CreateLogger<InspectionProcessorFunction>();
        _blobConnectionString = configuration["BlobStorageConnectionString"] ?? string.Empty;
        _containerName = configuration["InspectionContainerName"] ?? string.Empty;
        _emailConnectionString = configuration["AcsEmailConnectionString"] ?? string.Empty;
        _emailFrom = configuration["AcsEmailFrom"] ?? string.Empty;
        _emailTo = configuration["AcsEmailTo"] ?? string.Empty;
        _inspectionApiClient = inspectionApiClient;
    }

    [Function("InspectionProcessor")]
    public async Task Run(
        [QueueTrigger("%InspectionQueueName%", Connection = "StorageConnectionString")]
        string message,
        CancellationToken cancellationToken)
    {
        QueueMessage? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<QueueMessage>(message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize queue message: {message}", message);
        }

        if (payload?.sessionId is null || payload.sessionId.Length == 0)
        {
            _logger.LogWarning("Queue message missing SessionId.");
            return;
        }

        if (_blobConnectionString.Length == 0 || _containerName.Length == 0)
        {
            _logger.LogError("Blob settings are missing. Set BlobStorageConnectionString and InspectionContainerName.");
            return;
        }


        try
        {
            var inspectionJson = await _inspectionApiClient.GetInspectionAsync(payload.sessionId, cancellationToken);
            _logger.LogInformation("Loaded inspection from API for SessionId {sessionId}.", payload.sessionId);
            _logger.LogInformation("Inspection payload: {payload}", inspectionJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inspection from API for SessionId {sessionId}.", payload.sessionId);
            return;
        }


        var containerClient = new BlobContainerClient(_blobConnectionString, _containerName);
        var blobClient = containerClient.GetBlobClient(payload.sessionId + ".json");

        string blobJson;
        try
        {
            var download = await blobClient.DownloadContentAsync();
            blobJson = download.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blob for SessionId {sessionId}.", payload.sessionId);
            return;
        }

        if (_emailConnectionString.Length == 0 || _emailFrom.Length == 0 || _emailTo.Length == 0)
        {
            _logger.LogError("Email settings are missing. Set AcsEmailConnectionString, AcsEmailFrom, and AcsEmailTo.");
            return;
        }

        try
        {
            var emailClient = new EmailClient(_emailConnectionString);
            var content = new EmailContent($"Inspection payload for SessionId {payload.sessionId}")
            {
                PlainText = blobJson
            };
            var recipients = new EmailRecipients(new[] { new EmailAddress(_emailTo) });
            var emailMessage = new EmailMessage(_emailFrom, recipients, content);
            await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for SessionId {sessionId}.", payload.sessionId);
            return;
        }

        

        _logger.LogInformation("Loaded inspection request for SessionId {sessionId}.", payload.sessionId);
        _logger.LogInformation("Payload: {payload}", JsonSerializer.Serialize(blobJson));
    }
}

public sealed record QueueMessage(string? sessionId);

public sealed record ReceiveInspectionRequest(
    string? SessionId,
    string? UserId,
    string? Name,
    Dictionary<string, string>? QueryParams
);
