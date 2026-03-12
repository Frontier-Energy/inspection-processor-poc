using InspectionProcessor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InspectionProcessor.Tests.Services;

public sealed class InspectionQueueMessageParserTests
{
    [Fact]
    public void GivenAValidQueuePayload_WhenParsing_ThenItReturnsTheSessionId()
    {
        var logger = new Mock<ILogger<InspectionQueueMessageParser>>();
        var sut = new InspectionQueueMessageParser(logger.Object);

        var result = sut.Parse("{\"sessionId\":\"session-123\"}");

        Assert.NotNull(result);
        Assert.Equal("session-123", result.SessionId);
    }

    [Fact]
    public void GivenAnInvalidQueuePayload_WhenParsing_ThenItReturnsNull()
    {
        var logger = new Mock<ILogger<InspectionQueueMessageParser>>();
        var sut = new InspectionQueueMessageParser(logger.Object);

        var result = sut.Parse("not-json");

        Assert.Null(result);
    }
}
