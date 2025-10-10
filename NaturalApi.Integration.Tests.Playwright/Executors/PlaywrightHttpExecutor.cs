using Microsoft.Playwright;
using NaturalApi;
using NaturalApi.Integration.Tests.Playwright.Tests;

namespace NaturalApi.Integration.Tests.Playwright.Executors;

/// <summary>
/// Playwright-based implementation of IAuthenticatedHttpExecutor.
/// Uses Playwright's IAPIRequestContext for HTTP operations with authentication support.
/// </summary>
public class PlaywrightHttpExecutor : IAuthenticatedHttpExecutor
{
    private readonly IAPIRequestContext _apiRequestContext;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the PlaywrightHttpExecutor class.
    /// </summary>
    /// <param name="apiRequestContext">Playwright APIRequestContext instance</param>
    /// <param name="baseUrl">Base URL for requests</param>
    public PlaywrightHttpExecutor(IAPIRequestContext apiRequestContext, string baseUrl)
    {
        _apiRequestContext = apiRequestContext ?? throw new ArgumentNullException(nameof(apiRequestContext));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
    }

    /// <summary>
    /// Initializes a new instance of the PlaywrightHttpExecutor class with base URL.
    /// </summary>
    /// <param name="baseUrl">Base URL for the Playwright client</param>
    public PlaywrightHttpExecutor(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));
            
        _baseUrl = baseUrl;
        
        // Initialize Playwright synchronously
        var playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
        _apiRequestContext = playwright.APIRequest.NewContextAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Initializes a new instance of the PlaywrightHttpExecutor class with options.
    /// </summary>
    /// <param name="options">Playwright options containing configuration</param>
    public PlaywrightHttpExecutor(PlaywrightOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new ArgumentException("BaseUrl cannot be null or empty", nameof(options));

        _baseUrl = options.BaseUrl;
        
        // Initialize Playwright synchronously
        var playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
        _apiRequestContext = playwright.APIRequest.NewContextAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an HTTP request based on the provided specification.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <returns>Result context with response data and validation methods</returns>
    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        try
        {
            // Build the URL with path and query parameters
            var url = BuildUrl(spec);
            
            // Create Playwright request options
            var requestOptions = new APIRequestContextOptions
            {
                Method = GetPlaywrightMethod(spec.Method),
                Headers = spec.Headers.ToDictionary(h => h.Key, h => h.Value),
                Data = GetRequestBody(spec)?.ToString(),
                Timeout = (float?)(spec.Timeout?.TotalMilliseconds ?? 30000)
            };

            // Add query parameters
            if (spec.QueryParams.Count > 0)
            {
                var queryString = string.Join("&", spec.QueryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}"));
                url += (url.Contains("?") ? "&" : "?") + queryString;
            }

            // Add cookies if specified
            if (spec.Cookies != null && spec.Cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", spec.Cookies.Select(c => $"{c.Key}={c.Value}"));
                var headersDict = requestOptions.Headers.ToDictionary(h => h.Key, h => h.Value);
                if (headersDict.ContainsKey("Cookie"))
                {
                    headersDict["Cookie"] += "; " + cookieHeader;
                }
                else
                {
                    headersDict["Cookie"] = cookieHeader;
                }
                requestOptions.Headers = headersDict;
            }
            
            // Execute request based on method
            var response = ExecuteRequestByMethod(url, requestOptions, spec.Method);
            
            return new PlaywrightApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            throw new ApiExecutionException("Error during Playwright request execution", ex, spec);
        }
    }

    /// <summary>
    /// Executes an HTTP request with authentication support.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <param name="authProvider">Authentication provider for token resolution</param>
    /// <param name="username">Username context for per-user authentication</param>
    /// <param name="password">Password context for authentication</param>
    /// <param name="suppressAuth">Whether to suppress authentication for this request</param>
    /// <returns>Result context with response data and validation methods</returns>
    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, string? password, bool suppressAuth)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        try
        {
            // Build the URL with path and query parameters
            var url = BuildUrl(spec);
            
            // Create Playwright request options
            var requestOptions = new APIRequestContextOptions
            {
                Method = GetPlaywrightMethod(spec.Method),
                Headers = spec.Headers.ToDictionary(h => h.Key, h => h.Value),
                Data = GetRequestBody(spec)?.ToString(),
                Timeout = (float?)(spec.Timeout?.TotalMilliseconds ?? 30000)
            };

            // Add query parameters
            if (spec.QueryParams.Count > 0)
            {
                var queryString = string.Join("&", spec.QueryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}"));
                url += (url.Contains("?") ? "&" : "?") + queryString;
            }

            // Add cookies if specified
            if (spec.Cookies != null && spec.Cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", spec.Cookies.Select(c => $"{c.Key}={c.Value}"));
                var headersDict = requestOptions.Headers.ToDictionary(h => h.Key, h => h.Value);
                if (headersDict.ContainsKey("Cookie"))
                {
                    headersDict["Cookie"] += "; " + cookieHeader;
                }
                else
                {
                    headersDict["Cookie"] = cookieHeader;
                }
                requestOptions.Headers = headersDict;
            }

            // Handle authentication
            if (!suppressAuth && authProvider != null)
            {
                var token = await authProvider.GetAuthTokenAsync(username, password);
                if (!string.IsNullOrEmpty(token))
                {
                    // Add Bearer token to headers
                    var headersDict = requestOptions.Headers.ToDictionary(h => h.Key, h => h.Value);
                    headersDict["Authorization"] = $"Bearer {token}";
                    requestOptions.Headers = headersDict;
                }
            }
            
            // Execute request based on method
            var response = ExecuteRequestByMethod(url, requestOptions, spec.Method);
            
            return new PlaywrightApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            throw new ApiExecutionException("Error during Playwright request execution", ex, spec);
        }
    }

    /// <summary>
    /// Builds the full URL with path and query parameters.
    /// </summary>
    /// <param name="spec">Request specification</param>
    /// <returns>Complete URL</returns>
    private string BuildUrl(ApiRequestSpec spec)
    {
        var url = spec.Endpoint;
        
        // Replace path parameters
        foreach (var param in spec.PathParams)
        {
            url = url.Replace($"{{{param.Key}}}", param.Value?.ToString() ?? "");
        }
        
        // If the URL is already absolute, return it as-is
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }
        
        // For relative URLs, combine with base URL
        var baseUri = new Uri(_baseUrl);
        return new Uri(baseUri, url).ToString();
    }

    /// <summary>
    /// Converts HttpMethod to Playwright method string.
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <returns>Playwright method string</returns>
    private string GetPlaywrightMethod(HttpMethod method)
    {
        return method.Method.ToUpper();
    }

    /// <summary>
    /// Gets the request body for POST, PUT, PATCH requests.
    /// </summary>
    /// <param name="spec">Request specification</param>
    /// <returns>Request body or null</returns>
    private object? GetRequestBody(ApiRequestSpec spec)
    {
        if (spec.Body != null && (spec.Method == HttpMethod.Post || 
                                spec.Method == HttpMethod.Put || 
                                spec.Method == HttpMethod.Patch))
        {
            return spec.Body;
        }
        return null;
    }

    /// <summary>
    /// Executes the request using the appropriate Playwright method.
    /// </summary>
    /// <param name="url">Request URL</param>
    /// <param name="options">Request options</param>
    /// <param name="method">HTTP method</param>
    /// <returns>API response</returns>
    private IAPIResponse ExecuteRequestByMethod(string url, APIRequestContextOptions options, HttpMethod method)
    {
        return method.Method.ToUpper() switch
        {
            "GET" => _apiRequestContext.GetAsync(url, options).GetAwaiter().GetResult(),
            "POST" => _apiRequestContext.PostAsync(url, options).GetAwaiter().GetResult(),
            "PUT" => _apiRequestContext.PutAsync(url, options).GetAwaiter().GetResult(),
            "PATCH" => _apiRequestContext.PatchAsync(url, options).GetAwaiter().GetResult(),
            "DELETE" => _apiRequestContext.DeleteAsync(url, options).GetAwaiter().GetResult(),
            _ => throw new NotSupportedException($"HTTP method {method.Method} is not supported")
        };
    }
}
