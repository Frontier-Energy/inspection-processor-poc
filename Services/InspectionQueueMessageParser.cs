using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Services;

public interface IInspectionQueueMessageParser
{
    QueueMessage? Parse(string message);
}

public sealed class InspectionQueueMessageParser : IInspectionQueueMessageParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<InspectionQueueMessageParser> _logger;

    public InspectionQueueMessageParser(ILogger<InspectionQueueMessageParser> logger)
    {
        _logger = logger;
    }

    public QueueMessage? Parse(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<QueueMessage>(message, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize queue message: {message}", message);
            return null;
        }
    }
}
