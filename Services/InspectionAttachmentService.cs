using Microsoft.Extensions.Logging;

namespace InspectionProcessor.Services;

public interface IInspectionAttachmentService
{
    Task<IReadOnlyCollection<InspectionEmailAttachment>> LoadAttachmentsAsync(
        string fallbackSessionId,
        IReadOnlyCollection<InspectionFileReference>? files,
        CancellationToken cancellationToken);
}

public sealed class InspectionAttachmentService : IInspectionAttachmentService
{
    private readonly IInspectionApiClient _inspectionApiClient;
    private readonly ILogger<InspectionAttachmentService> _logger;

    public InspectionAttachmentService(
        IInspectionApiClient inspectionApiClient,
        ILogger<InspectionAttachmentService> logger)
    {
        _inspectionApiClient = inspectionApiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<InspectionEmailAttachment>> LoadAttachmentsAsync(
        string fallbackSessionId,
        IReadOnlyCollection<InspectionFileReference>? files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return [];
        }

        var attachments = new List<InspectionEmailAttachment>();

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                _logger.LogWarning(
                    "Inspection file missing FileName for SessionId {sessionId}.",
                    fallbackSessionId);
                continue;
            }

            var fileSessionId = string.IsNullOrWhiteSpace(file.SessionId)
                ? fallbackSessionId
                : file.SessionId;

            if (string.IsNullOrWhiteSpace(fileSessionId))
            {
                _logger.LogWarning("Inspection file {fileName} missing SessionId.", file.FileName);
                continue;
            }

            try
            {
                var filePayload = await _inspectionApiClient.GetFileAsync(
                    fileSessionId,
                    file.FileName,
                    cancellationToken);

                attachments.Add(new InspectionEmailAttachment(
                    filePayload.FileName,
                    filePayload.ContentType,
                    filePayload.Content));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load file {fileName} for SessionId {sessionId}.",
                    file.FileName,
                    fileSessionId);
            }
        }

        return attachments;
    }
}
