using NaturalApi;
using NaturalApi.Reporter;

namespace NaturalApi.Integration.Tests.Common;

/// <summary>
/// Custom API implementation that uses a properly configured HttpClient for authentication.
/// </summary>
public class CustomApi : IApi
{
    private readonly IHttpExecutor _httpExecutor;
    private readonly IApiDefaultsProvider? _defaults;
    private readonly HttpClient _httpClient;

    public CustomApi(IHttpExecutor httpExecutor, IApiDefaultsProvider? defaults, HttpClient httpClient, INaturalReporter? reporter = null)
    {
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
        _defaults = defaults;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public IApiContext For(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        // Additional validation for edge cases - only reject truly invalid patterns
        var trimmedEndpoint = endpoint.Trim();
        if (trimmedEndpoint == "" || (trimmedEndpoint.Length > 1 && trimmedEndpoint.All(c => c == '/')))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        // Determine the full endpoint URL
        string fullEndpoint;
        if (_defaults?.BaseUri != null && !endpoint.StartsWith("http"))
        {
            // Use defaults base URI
            fullEndpoint = $"{_defaults.BaseUri.ToString().TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }
        else
        {
            // Use endpoint as-is (absolute URL)
            fullEndpoint = endpoint;
        }

        // Create spec with defaults
        var spec = new ApiRequestSpec(
            fullEndpoint,
            HttpMethod.Get, // Default method, will be overridden by verb methods
            _defaults?.DefaultHeaders ?? new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            _defaults?.Timeout);

        // Use authenticated executor with our configured HttpClient
        var executor = _defaults?.AuthProvider != null
            ? new AuthenticatedHttpClientExecutor(_httpClient)
            : _httpExecutor;
        
        return new ApiContext(spec, executor, _defaults?.AuthProvider);
    }
}
