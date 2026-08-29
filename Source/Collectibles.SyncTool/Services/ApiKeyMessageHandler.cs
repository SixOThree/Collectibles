using System.Net.Http;

namespace Collectibles.SyncTool.Services;

/// <summary>
/// Holds the API key currently configured for the sync session.
/// </summary>
/// <remarks>
/// The key used to be pushed onto <c>HttpClient.DefaultRequestHeaders</c>, which is not
/// thread-safe: reconfiguring while another operation had requests in flight mutated a
/// collection those requests were reading. A single reference assignment here is atomic,
/// and the handler below stamps the header per request instead.
/// </remarks>
public class ApiKeyProvider
{
    private volatile string? _apiKey;

    public string? ApiKey
    {
        get => _apiKey;
        set => _apiKey = value;
    }
}

/// <summary>
/// Attaches the configured API key to each outgoing request.
/// </summary>
public class ApiKeyMessageHandler : DelegatingHandler
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly ApiKeyProvider _apiKeyProvider;

    public ApiKeyMessageHandler(ApiKeyProvider apiKeyProvider)
        : base(new HttpClientHandler())
    {
        _apiKeyProvider = apiKeyProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _apiKeyProvider.ApiKey;

        if (!string.IsNullOrEmpty(apiKey) && !request.Headers.Contains(ApiKeyHeaderName))
        {
            request.Headers.Add(ApiKeyHeaderName, apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
