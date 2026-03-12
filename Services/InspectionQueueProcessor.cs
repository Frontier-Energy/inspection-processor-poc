using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Services;

public interface IInspectionQueueProcessor
{
    Task ProcessAsync(string message, CancellationToken cancellationToken);
}

public sealed class InspectionQueueProcessor : IInspectionQueueProcessor
{
    private readonly IInspectionQueueMessageParser _messageParser;
    private readonly IInspectionApiClient _inspectionApiClient;
    private readonly IInspectionAttachmentService _attachmentService;
    private readonly IInspectionEmailRenderer _emailRenderer;
    private readonly IInspectionEmailSender _emailSender;
    private readonly ILogger<InspectionQueueProcessor> _logger;

    public InspectionQueueProcessor(
        IInspectionQueueMessageParser messageParser,
        IInspectionApiClient inspectionApiClient,
        IInspectionAttachmentService attachmentService,
        IInspectionEmailRenderer emailRenderer,
        IInspectionEmailSender emailSender,
        ILogger<InspectionQueueProcessor> logger)
    {
        _messageParser = messageParser;
        _inspectionApiClient = inspectionApiClient;
        _attachmentService = attachmentService;
        _emailRenderer = emailRenderer;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task ProcessAsync(string message, CancellationToken cancellationToken)
    {
        var queueMessage = _messageParser.Parse(message);
        var sessionId = queueMessage?.SessionId;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("Queue message missing SessionId.");
            return;
        }

        GetInspectionResponse inspection;
        try
        {
            inspection = await _inspectionApiClient.GetInspectionAsync(sessionId, cancellationToken);
            _logger.LogInformation("Loaded inspection from API for SessionId {sessionId}.", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inspection from API for SessionId {sessionId}.", sessionId);
            return;
        }

        var user = await TryGetUserAsync(inspection.UserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user?.Email))
        {
            _logger.LogError("User email address is missing for SessionId {sessionId}.", sessionId);
            return;
        }

        var attachments = await _attachmentService.LoadAttachmentsAsync(
            sessionId,
            inspection.Files,
            cancellationToken);

        var emailMessage = new InspectionEmailMessage(
            user.Email,
            $"Inspection payload for SessionId {sessionId}",
            _emailRenderer.RenderHtml(inspection, user),
            attachments);

        try
        {
            await _emailSender.SendAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for SessionId {sessionId}.", sessionId);
            return;
        }

        _logger.LogInformation("Loaded inspection request for SessionId {sessionId}.", sessionId);
        _logger.LogInformation("Payload: {payload}", JsonSerializer.Serialize(inspection));
    }

    private async Task<UserModel?> TryGetUserAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            var userResponse = await _inspectionApiClient.GetUserAsync(userId, cancellationToken);
            return userResponse.User;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user info for UserId {userId}.", userId);
            return null;
        }
    }
}
