using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Functions;

public class InspectionProcessorFunction
{
    private readonly ILogger _logger;
    private readonly string _blobConnectionString;
    private readonly string _containerName;

    public InspectionProcessorFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<InspectionProcessorFunction>();
        _blobConnectionString = configuration["BlobStorageConnectionString"] ?? string.Empty;
        _containerName = configuration["InspectionContainerName"] ?? string.Empty;
    }

    [Function("InspectionProcessor")]
    public async Task Run(
        [QueueTrigger("%InspectionQueueName%", Connection = "StorageConnectionString")]
        string message)
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

        ReceiveInspectionRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<ReceiveInspectionRequest>(blobJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize blob payload for SessionId {sessionId}.", payload.sessionId);
        }

        if (request is null)
        {
            _logger.LogWarning("Blob payload is empty or invalid for SessionId {sessionId}.", payload.sessionId);
            return;
        }

        _logger.LogError("Loaded inspection request for SessionId {sessionId}.", payload.sessionId);
        _logger.LogError("Payload: {payload}", JsonSerializer.Serialize(blobJson));
    }
}

public sealed record QueueMessage(string? sessionId);

public sealed record ReceiveInspectionRequest(
    string? SessionId,
    string? UserId,
    string? Name,
    Dictionary<string, string>? QueryParams
);
