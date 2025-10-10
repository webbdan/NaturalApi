using System.Net.Http.Headers;

namespace NaturalApi;

/// <summary>
/// HttpClient-based implementation of IAuthenticatedHttpExecutor.
/// Executes HTTP requests with authentication support.
/// </summary>
public class AuthenticatedHttpClientExecutor : IAuthenticatedHttpExecutor
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the AuthenticatedHttpClientExecutor class.
    /// </summary>
    /// <param name="httpClient">HttpClient instance for making requests</param>
    public AuthenticatedHttpClientExecutor(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Executes an HTTP request based on the provided specification.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <returns>Result context with response data and validation methods</returns>
    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        // For backward compatibility, execute without authentication
        return ExecuteAsync(spec, null, null, null, true).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an HTTP request with authentication resolution.
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
            
            // Create the HTTP request message
            var request = new HttpRequestMessage(spec.Method, url);
            
            // Add headers (excluding Content-Type which goes on content)
            foreach (var header in spec.Headers)
            {
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip Content-Type here, it will be set on content
                    continue;
                }
                request.Headers.Add(header.Key, header.Value);
            }
            
            // Add cookies from request specification
            if (spec.Cookies != null && spec.Cookies.Count > 0)
            {
                var cookieValues = spec.Cookies.Select(c => $"{c.Key}={c.Value}");
                request.Headers.Add("Cookie", string.Join("; ", cookieValues));
            }

            // Handle authentication
            if (!suppressAuth && authProvider != null)
            {
                var token = await authProvider.GetAuthTokenAsync(username, password);
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            
            // Add body for POST, PUT, PATCH requests
            if (spec.Body != null && (spec.Method == HttpMethod.Post || spec.Method == HttpMethod.Put || spec.Method == HttpMethod.Patch))
            {
                var json = System.Text.Json.JsonSerializer.Serialize(spec.Body);
                // Extract just the media type from Content-Type header (remove charset if present)
                var contentType = spec.Headers.ContainsKey("Content-Type") 
                    ? spec.Headers["Content-Type"].Split(';')[0].Trim() 
                    : "application/json";
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, contentType);
            }
            
            // Execute the request with timeout if specified
            HttpResponseMessage response;
            if (spec.Timeout.HasValue)
            {
                using var cts = new CancellationTokenSource(spec.Timeout.Value);
                response = await _httpClient.SendAsync(request, cts.Token);
            }
            else
            {
                response = await _httpClient.SendAsync(request);
            }
            
            return new ApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            // Wrap any exception with full request context
            throw new ApiExecutionException("Error during HTTP request execution", ex, spec);
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
            url = url.Replace($"{{{param.Key}}}", param.Value.ToString());
        }
        
        // Add query parameters
        if (spec.QueryParams.Count > 0)
        {
            var queryString = string.Join("&", spec.QueryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value.ToString() ?? "")}"));
            url += (url.Contains("?") ? "&" : "?") + queryString;
        }
        
        return url;
    }
}
