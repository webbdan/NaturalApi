namespace NaturalApi;

/// <summary>
/// Main entry point for the NaturalApi fluent DSL.
/// Provides the For() method to create API contexts.
/// </summary>
public class Api : IApi
{
    private readonly IHttpExecutor _httpExecutor;

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

        // Combine base URL with endpoint if base URL is provided
        var fullEndpoint = _baseUrl != null && !endpoint.StartsWith("http") 
            ? $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}"
            : endpoint;

        var spec = new ApiRequestSpec(
            fullEndpoint,
            HttpMethod.Get, // Default method, will be overridden by verb methods
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);

        return new ApiContext(spec, _httpExecutor);
    }
}
