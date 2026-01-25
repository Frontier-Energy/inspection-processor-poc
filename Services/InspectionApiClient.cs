using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectionProcessor.Services;

public interface IInspectionApiClient
{
    Task<string> GetInspectionAsync(string sessionId, CancellationToken cancellationToken);
}

public sealed class InspectionApiClient : IInspectionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly InspectionApiOptions _options;
    private readonly ILogger<InspectionApiClient> _logger;

    public InspectionApiClient(
        HttpClient httpClient,
        IOptions<InspectionApiOptions> options,
        ILogger<InspectionApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetInspectionAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Inspection API base URL is missing. Set InspectionApi:BaseUrl.");
        }

        var path = string.IsNullOrWhiteSpace(_options.GetPath)
            ? "QHVAC/GetInspection/"
            : _options.GetPath;

        var requestUri = $"{path}?sessionId={Uri.EscapeDataString(sessionId)}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Inspection API returned {statusCode}. Body: {body}",
                response.StatusCode,
                body);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
