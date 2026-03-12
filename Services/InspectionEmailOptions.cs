namespace InspectionProcessor.Services;

public sealed class InspectionEmailOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
}
