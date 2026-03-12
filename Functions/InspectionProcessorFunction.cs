using InspectionProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Functions;

public class InspectionProcessorFunction
{
    private readonly ILogger<InspectionProcessorFunction> _logger;
    private readonly IInspectionQueueProcessor _queueProcessor;

    public InspectionProcessorFunction(
        ILogger<InspectionProcessorFunction> logger,
        IInspectionQueueProcessor queueProcessor)
    {
        _logger = logger;
        _queueProcessor = queueProcessor;
    }

    [Function("InspectionProcessor")]
    public async Task Run(
        [QueueTrigger("%InspectionQueueName%", Connection = "StorageConnectionString")]
        string message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing inspection queue message.");
        await _queueProcessor.ProcessAsync(message, cancellationToken);
    }
}
