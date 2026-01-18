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
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("InspectionProcessor timer fired at: {time}", DateTimeOffset.Now);

        if (timer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {next}", timer.ScheduleStatus.Next);
        }
    }
}
