namespace InspectionProcessor.Services;

public sealed record InspectionEmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content
);

public sealed record InspectionEmailMessage(
    string ToAddress,
    string Subject,
    string HtmlBody,
    IReadOnlyCollection<InspectionEmailAttachment> Attachments
);
