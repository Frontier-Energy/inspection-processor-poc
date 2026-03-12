using InspectionProcessor.Functions;
using InspectionProcessor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InspectionProcessor.Tests.Functions;

public sealed class InspectionProcessorFunctionTests
{
    [Fact]
    public async Task GivenQueueMessage_WhenRunIsInvoked_ThenItProcessesTheMessage()
    {
        var logger = new Mock<ILogger<InspectionProcessorFunction>>();
        var queueProcessor = new Mock<IInspectionQueueProcessor>();
        var sut = new InspectionProcessorFunction(logger.Object, queueProcessor.Object);
        const string message = "{\"sessionId\":\"session-123\"}";
        using var cancellationSource = new CancellationTokenSource();

        await sut.Run(message, cancellationSource.Token);

        queueProcessor.Verify(
            processor => processor.ProcessAsync(message, cancellationSource.Token),
            Times.Once);
    }
}
