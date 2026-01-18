using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Functions;

public class InspectionProcessorFunction
{
    private readonly ILogger _logger;

    public InspectionProcessorFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<InspectionProcessorFunction>();
    }

    [Function("InspectionProcessor")]
    public void Run(
        [QueueTrigger("%InspectionQueueName%", Connection = "StorageConnectionString")]
        string message)
    {
        _logger.LogInformation("InspectionProcessor queue message received: {message}", message);
    }
}
