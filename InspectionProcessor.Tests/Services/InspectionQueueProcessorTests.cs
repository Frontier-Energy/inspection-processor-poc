using InspectionProcessor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InspectionProcessor.Tests.Services;

public sealed class InspectionQueueProcessorTests
{
    [Fact]
    public async Task GivenAMessageWithoutASessionId_WhenProcessing_ThenItStopsBeforeCallingExternalDependencies()
    {
        var messageParser = new Mock<IInspectionQueueMessageParser>();
        var apiClient = new Mock<IInspectionApiClient>();
        var attachmentService = new Mock<IInspectionAttachmentService>();
        var emailRenderer = new Mock<IInspectionEmailRenderer>();
        var emailSender = new Mock<IInspectionEmailSender>();
        var logger = new Mock<ILogger<InspectionQueueProcessor>>();
        var sut = new InspectionQueueProcessor(
            messageParser.Object,
            apiClient.Object,
            attachmentService.Object,
            emailRenderer.Object,
            emailSender.Object,
            logger.Object);

        messageParser.Setup(parser => parser.Parse("payload")).Returns(new QueueMessage(null));

        await sut.ProcessAsync("payload", CancellationToken.None);

        apiClient.VerifyNoOtherCalls();
        attachmentService.VerifyNoOtherCalls();
        emailRenderer.VerifyNoOtherCalls();
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenAnInspectionWithoutAUserEmail_WhenProcessing_ThenItDoesNotSendAnEmail()
    {
        var messageParser = new Mock<IInspectionQueueMessageParser>();
        var apiClient = new Mock<IInspectionApiClient>();
        var attachmentService = new Mock<IInspectionAttachmentService>();
        var emailRenderer = new Mock<IInspectionEmailRenderer>();
        var emailSender = new Mock<IInspectionEmailSender>();
        var logger = new Mock<ILogger<InspectionQueueProcessor>>();
        var sut = new InspectionQueueProcessor(
            messageParser.Object,
            apiClient.Object,
            attachmentService.Object,
            emailRenderer.Object,
            emailSender.Object,
            logger.Object);
        var inspection = new GetInspectionResponse("session-123", "user-456", "Inspection", [], []);

        messageParser.Setup(parser => parser.Parse("payload")).Returns(new QueueMessage("session-123"));
        apiClient
            .Setup(client => client.GetInspectionAsync("session-123", CancellationToken.None))
            .ReturnsAsync(inspection);
        apiClient
            .Setup(client => client.GetUserAsync("user-456", CancellationToken.None))
            .ReturnsAsync(new GetUserResponse(new UserModel("user-456", null, "Jane", "Doe")));

        await sut.ProcessAsync("payload", CancellationToken.None);

        attachmentService.VerifyNoOtherCalls();
        emailRenderer.VerifyNoOtherCalls();
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenAValidInspectionMessage_WhenProcessing_ThenItSendsTheInspectionEmail()
    {
        var messageParser = new Mock<IInspectionQueueMessageParser>();
        var apiClient = new Mock<IInspectionApiClient>();
        var attachmentService = new Mock<IInspectionAttachmentService>();
        var emailRenderer = new Mock<IInspectionEmailRenderer>();
        var emailSender = new Mock<IInspectionEmailSender>();
        var logger = new Mock<ILogger<InspectionQueueProcessor>>();
        var sut = new InspectionQueueProcessor(
            messageParser.Object,
            apiClient.Object,
            attachmentService.Object,
            emailRenderer.Object,
            emailSender.Object,
            logger.Object);
        var inspection = new GetInspectionResponse(
            "session-123",
            "user-456",
            "Inspection Name",
            new Dictionary<string, string> { ["site"] = "Dallas" },
            [new InspectionFileReference("report.pdf", "session-123", "application/pdf")]);
        var user = new UserModel("user-456", "user@example.com", "Jane", "Doe");
        var attachments = new[]
        {
            new InspectionEmailAttachment("report.pdf", "application/pdf", [1, 2, 3])
        };

        messageParser.Setup(parser => parser.Parse("payload")).Returns(new QueueMessage("session-123"));
        apiClient
            .Setup(client => client.GetInspectionAsync("session-123", CancellationToken.None))
            .ReturnsAsync(inspection);
        apiClient
            .Setup(client => client.GetUserAsync("user-456", CancellationToken.None))
            .ReturnsAsync(new GetUserResponse(user));
        attachmentService
            .Setup(service => service.LoadAttachmentsAsync("session-123", inspection.Files, CancellationToken.None))
            .ReturnsAsync(attachments);
        emailRenderer
            .Setup(renderer => renderer.RenderHtml(inspection, user))
            .Returns("<html>inspection</html>");

        await sut.ProcessAsync("payload", CancellationToken.None);

        emailSender.Verify(
            sender => sender.SendAsync(
                It.Is<InspectionEmailMessage>(message =>
                    message.ToAddress == "user@example.com"
                    && message.Subject == "Inspection payload for SessionId session-123"
                    && message.HtmlBody == "<html>inspection</html>"
                    && ReferenceEquals(message.Attachments, attachments)),
                CancellationToken.None),
            Times.Once);
    }
}
