using NaturalApi.Reporter;
using Spectre.Console;

namespace NaturalApi;

/// <summary>
/// Main entry point for the NaturalApi fluent DSL.
/// Provides the For() method to create API contexts.
/// </summary>
public class Api : IApi
{
    private IHttpExecutor _httpExecutor;
    private readonly IApiDefaultsProvider? _defaults;
    private readonly string? _baseUrl;
    private INaturalReporter _reporter = new DefaultReporter();

    public INaturalReporter Reporter
    {
        get { return _reporter; }
        set { _reporter = value; _httpExecutor.Reporter = _reporter; }
    }

    /// <summary>
    /// Initializes a new instance of the Api class.
    /// </summary>
    /// <param name="httpExecutor">HTTP executor for making requests</param>
    public Api(IHttpExecutor httpExecutor)
    {
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
        // prefer executor reporter if present
        _reporter = httpExecutor.Reporter ?? new DefaultReporter();
    }

    /// <summary>
    /// Initializes a new instance of the Api class with default HttpClient.
    /// This is the simplest way to use NaturalApi - just use absolute URLs directly.
    /// </summary>
    public Api()
    {
        _reporter = new DefaultReporter();
        _httpExecutor = new HttpClientExecutor(new HttpClient(), _reporter);
    }

    /// <summary>
    /// Initializes a new instance of the Api class with a base URL.
    /// </summary>
    /// <param name="baseUrl">Base URL for all requests</param>
    public Api(string baseUrl, INaturalReporter? reporter = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));
        
        _baseUrl = baseUrl;
        _reporter = reporter ?? new DefaultReporter();
        _httpExecutor = new HttpClientExecutor(new HttpClient(), _reporter);
    }

    public Api(INaturalReporter reporter)
    {
        _reporter = reporter ?? new DefaultReporter();
        _httpExecutor = new HttpClientExecutor(new HttpClient(), _reporter);
    }

    /// <summary>
    /// Initializes a new instance of the Api class with defaults provider.
    /// </summary>
    /// <param name="httpExecutor">HTTP executor for making requests</param>
    /// <param name="defaults">Default configuration provider</param>
    public Api(IHttpExecutor httpExecutor, IApiDefaultsProvider defaults, INaturalReporter? reporter = null)
    {
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        _reporter = reporter ?? _defaults.Reporter ?? _httpExecutor.Reporter ?? new DefaultReporter();
    }

    /// <summary>
    /// Initializes a new instance of the Api class with defaults provider and HttpClient.
    /// This constructor automatically handles authentication when an auth provider is present.
    /// </summary>
    /// <param name="defaults">Default configuration provider</param>
    /// <param name="httpClient">HttpClient for making requests</param>
    public Api(IApiDefaultsProvider defaults, HttpClient httpClient)
    {
        if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
        if (defaults == null) throw new ArgumentNullException(nameof(defaults));
        
        _defaults = defaults;
        
        // Choose reporter from defaults or fallback
        _reporter = defaults.Reporter ?? new DefaultReporter();
        
        // Automatically choose the right executor based on authentication
        _httpExecutor = defaults.AuthProvider != null 
            ? new AuthenticatedHttpClientExecutor(httpClient, _reporter)
            : new HttpClientExecutor(httpClient, _reporter);
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

        // Validate that the final URL is well-formed
        // For absolute URLs, always validate them (but allow path parameters)
        // For relative URLs, only validate if we have a base URI (meaning we're combining URLs)
        if (endpoint.StartsWith("http"))
        {
            // Allow path parameters like {id} - they will be replaced later
            var urlWithoutParams = fullEndpoint.Replace("{", "").Replace("}", "");
            if (!Uri.IsWellFormedUriString(urlWithoutParams, UriKind.Absolute))
            {
                throw new ArgumentException($"The URL '{fullEndpoint}' is not a valid absolute URI", nameof(endpoint));
            }
        }
        else if (_defaults?.BaseUri != null || _baseUrl != null)
        {
            // Allow path parameters like {id} - they will be replaced later
            var urlWithoutParams = fullEndpoint.Replace("{", "").Replace("}", "");
            if (!Uri.IsWellFormedUriString(urlWithoutParams, UriKind.Absolute))
            {
                throw new ArgumentException($"The combined URL '{fullEndpoint}' is not a valid absolute URI", nameof(endpoint));
            }
        }
        else
        {
            // For relative URLs without base URI, validate that they are reasonable
            // Check for obviously malformed URLs
            if (endpoint.Contains("://") || endpoint.Contains(" ") || 
                endpoint.StartsWith("://") || endpoint.EndsWith("://") ||
                endpoint.Contains("[") && !endpoint.Contains("]") ||
                endpoint.StartsWith("http") && !endpoint.StartsWith("http://") && !endpoint.StartsWith("https://") ||
                endpoint.Contains("-") && !endpoint.Contains("/") && !endpoint.Contains("."))
            {
                throw new ArgumentException($"The endpoint '{endpoint}' appears to be malformed", nameof(endpoint));
            }
        }

        // Create spec with defaults
        var spec = new ApiRequestSpec(
            fullEndpoint,
            HttpMethod.Get, // Default method, will be overridden by verb methods
            _defaults?.DefaultHeaders ?? new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            _defaults?.Timeout,
            Reporter: _defaults?.Reporter ?? _reporter);

        // The executor is already configured correctly in the constructor
        return new ApiContext(spec, _httpExecutor, _defaults?.AuthProvider);
    }
}
