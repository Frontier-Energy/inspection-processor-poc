namespace InspectionProcessor.Services;

public sealed class InspectionApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string GetPath { get; set; } = "QHVAC/GetInspection/";
    public string GetFilePath { get; set; } = "QHVAC/GetFile";
}
