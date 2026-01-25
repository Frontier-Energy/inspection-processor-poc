using System.Text.Json;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using InspectionProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Functions;

public class InspectionProcessorFunction
{
    private readonly ILogger _logger;
    private readonly string _blobConnectionString;
    private readonly string _containerName;
    private readonly string _emailConnectionString;
    private readonly string _emailFrom;
    private readonly IInspectionApiClient _inspectionApiClient;
    private readonly InspectionEmailRenderer _emailRenderer;

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
        _inspectionApiClient = inspectionApiClient;
        _emailRenderer = new InspectionEmailRenderer();
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


        GetInspectionResponse inspection = null!;
        UserModel? user = null;

        try
        {
            inspection = await _inspectionApiClient.GetInspectionAsync(payload.sessionId, cancellationToken);
          
            _logger.LogInformation("Loaded inspection from API for SessionId {sessionId}.", payload.sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inspection from API for SessionId {sessionId}.", payload.sessionId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(inspection.UserId))
        {
            try
            {
                var userResponse = await _inspectionApiClient.GetUserAsync(inspection.UserId, cancellationToken);
                user = userResponse.User;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user info for UserId {userId}.", inspection.UserId);
            }
        }

        string inspectionJson = JsonSerializer.Serialize(inspection);
       
        if (_emailConnectionString.Length == 0 || _emailFrom.Length == 0)
        {
            _logger.LogError("Email settings are missing. Set AcsEmailConnectionString and AcsEmailFrom.");
            return;
        }

        if (string.IsNullOrWhiteSpace(user?.Email))
        {
            _logger.LogError(
                "User email address is missing for SessionId {sessionId}.",
                payload.sessionId);
            return;
        }

        try
        {
            var emailClient = new EmailClient(_emailConnectionString);
            var content = new EmailContent($"Inspection payload for SessionId {payload.sessionId}")
            {
                Html = _emailRenderer.RenderHtml(inspection, user)
            };
            var recipients = new EmailRecipients(new[] { new EmailAddress(user.Email) });
            List<EmailAttachment>? attachments = null;
            if (inspection.Files is { Count: > 0 })
            {
                attachments = new List<EmailAttachment>();
                foreach (var file in inspection.Files)
                {
                    if (string.IsNullOrWhiteSpace(file.FileName))
                    {
                        _logger.LogWarning("Inspection file missing FileName for SessionId {sessionId}.", payload.sessionId);
                        continue;
                    }

                    var fileSessionId = string.IsNullOrWhiteSpace(file.SessionId) ? payload.sessionId : file.SessionId;
                    if (string.IsNullOrWhiteSpace(fileSessionId))
                    {
                        _logger.LogWarning("Inspection file {fileName} missing SessionId.", file.FileName);
                        continue;
                    }

                    try
                    {
                        var filePayload = await _inspectionApiClient.GetFileAsync(fileSessionId, file.FileName, cancellationToken);
                        attachments.Add(new EmailAttachment(
                            filePayload.FileName,
                            filePayload.ContentType,
                            BinaryData.FromBytes(filePayload.Content)));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to load file {fileName} for SessionId {sessionId}.",
                            file.FileName,
                            fileSessionId);
                    }
                }
            }

            var emailMessage = new EmailMessage(_emailFrom, recipients, content);
            if (attachments is { Count: > 0 })
            {
                if (emailMessage.Attachments is null)
                {
                    _logger.LogWarning("Email message attachments collection is not available.");
                }
                else
                {
                    foreach (var attachment in attachments)
                    {
                        emailMessage.Attachments.Add(attachment);
                    }
                }
            }
            await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for SessionId {sessionId}.", payload.sessionId);
            return;
        }

        

        _logger.LogInformation("Loaded inspection request for SessionId {sessionId}.", payload.sessionId);
        _logger.LogInformation("Payload: {payload}", inspectionJson);
    }
}

public sealed record QueueMessage(string? sessionId);

public sealed record ReceiveInspectionRequest(
    string? SessionId,
    string? UserId,
    string? Name,
    Dictionary<string, string>? QueryParams
);
