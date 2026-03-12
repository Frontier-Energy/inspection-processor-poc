using InspectionProcessor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InspectionProcessor.Tests.Services;

public sealed class InspectionAttachmentServiceTests
{
    [Fact]
    public async Task GivenNoFiles_WhenLoadingAttachments_ThenItReturnsAnEmptyCollection()
    {
        var apiClient = new Mock<IInspectionApiClient>();
        var logger = new Mock<ILogger<InspectionAttachmentService>>();
        var sut = new InspectionAttachmentService(apiClient.Object, logger.Object);

        var result = await sut.LoadAttachmentsAsync("session-123", null, CancellationToken.None);

        Assert.Empty(result);
        apiClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenFilesWithoutASessionId_WhenLoadingAttachments_ThenItUsesTheFallbackSessionId()
    {
        var apiClient = new Mock<IInspectionApiClient>();
        var logger = new Mock<ILogger<InspectionAttachmentService>>();
        var sut = new InspectionAttachmentService(apiClient.Object, logger.Object);
        var files = new[]
        {
            new InspectionFileReference("report.pdf", null, "application/pdf")
        };
        var filePayload = new InspectionFilePayload("report.pdf", "application/pdf", [1, 2, 3]);

        apiClient
            .Setup(client => client.GetFileAsync("fallback-session", "report.pdf", CancellationToken.None))
            .ReturnsAsync(filePayload);

        var result = await sut.LoadAttachmentsAsync("fallback-session", files, CancellationToken.None);

        var attachment = Assert.Single(result);
        Assert.Equal("report.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(filePayload.Content, attachment.Content);
    }

    [Fact]
    public async Task GivenAFileWithoutAName_WhenLoadingAttachments_ThenItSkipsTheFile()
    {
        var apiClient = new Mock<IInspectionApiClient>();
        var logger = new Mock<ILogger<InspectionAttachmentService>>();
        var sut = new InspectionAttachmentService(apiClient.Object, logger.Object);
        var files = new[]
        {
            new InspectionFileReference(null, "session-123", "application/pdf")
        };

        var result = await sut.LoadAttachmentsAsync("fallback-session", files, CancellationToken.None);

        Assert.Empty(result);
        apiClient.VerifyNoOtherCalls();
    }
}
