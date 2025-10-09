namespace NaturalApi;

/// <summary>
/// Main entry point for the NaturalApi fluent DSL.
/// Provides the For() method to create API contexts.
/// </summary>
public class Api : IApi
{
    private readonly IHttpExecutor _httpExecutor;
    private readonly IApiDefaultsProvider? _defaults;
    private readonly string? _baseUrl;

    /// <summary>
    /// Initializes a new instance of the Api class.
    /// </summary>
    /// <param name="httpExecutor">HTTP executor for making requests</param>
    public Api(IHttpExecutor httpExecutor)
    {
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
    }

    /// <summary>
    /// Initializes a new instance of the Api class with a base URL.
    /// </summary>
    /// <param name="baseUrl">Base URL for all requests</param>
    public Api(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));
        
        _baseUrl = baseUrl;
        _httpExecutor = new HttpClientExecutor(new HttpClient());
    }

    /// <summary>
    /// Initializes a new instance of the Api class with defaults provider.
    /// </summary>
    /// <param name="httpExecutor">HTTP executor for making requests</param>
    /// <param name="defaults">Default configuration provider</param>
    public Api(IHttpExecutor httpExecutor, IApiDefaultsProvider defaults)
    {
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
    }

    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <returns>An API context for building and executing requests</returns>
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
        else if (_baseUrl != null && !endpoint.StartsWith("http"))
        {
            // Use legacy base URL
            fullEndpoint = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
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

        // Use authenticated executor if available and not using a mock, otherwise use regular executor
        var executor = _defaults?.AuthProvider != null && _httpExecutor.GetType().Name != "MockHttpExecutor"
            ? new AuthenticatedHttpClientExecutor(new HttpClient())
            : _httpExecutor;
        
        return new ApiContext(spec, executor, _defaults?.AuthProvider);
    }
}
