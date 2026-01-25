namespace InspectionProcessor.Services;

public sealed record GetInspectionResponse(
    string? SessionId,
    string? UserId,
    string? Name,
    Dictionary<string, string>? QueryParams,
    List<InspectionFileReference>? Files
);

public sealed record InspectionFileReference(
    string? FileName,
    string? SessionId,
    string? FileType
);
