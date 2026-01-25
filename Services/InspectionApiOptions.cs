namespace InspectionProcessor.Services;

public sealed class InspectionApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string GetPath { get; set; } = "QHVAC/GetInspection/";
}
