using System.Text.Json.Serialization;

namespace InspectionProcessor.Services;

public sealed record QueueMessage([property: JsonPropertyName("sessionId")] string? SessionId);
