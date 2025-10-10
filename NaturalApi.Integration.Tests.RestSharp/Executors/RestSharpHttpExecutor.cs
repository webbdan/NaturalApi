using RestSharp;
using NaturalApi;

namespace NaturalApi.Integration.Tests.RestSharp.Executors;

/// <summary>
/// RestSharp-based implementation of IHttpExecutor.
/// Uses RestSharp's RestClient for HTTP operations.
/// </summary>
public class RestSharpHttpExecutor : IHttpExecutor
{
    private readonly RestClient _restClient;

    /// <summary>
    /// Initializes a new instance of the RestSharpHttpExecutor class.
    /// </summary>
    /// <param name="restClient">RestSharp RestClient instance</param>
    public RestSharpHttpExecutor(RestClient restClient)
    {
        _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
    }

    /// <summary>
    /// Initializes a new instance of the RestSharpHttpExecutor class with base URL.
    /// </summary>
    /// <param name="baseUrl">Base URL for the RestClient</param>
    public RestSharpHttpExecutor(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));
            
        _restClient = new RestClient(baseUrl);
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
            
            // Create RestSharp request
            var request = new RestRequest(url, GetRestSharpMethod(spec.Method));
            
            // Add headers
            foreach (var header in spec.Headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
            
            // Add query parameters
            foreach (var param in spec.QueryParams)
            {
                request.AddQueryParameter(param.Key, param.Value?.ToString() ?? "");
            }
            
            // Add body for POST, PUT, PATCH
            if (spec.Body != null && (spec.Method == HttpMethod.Post || 
                                    spec.Method == HttpMethod.Put || 
                                    spec.Method == HttpMethod.Patch))
            {
                request.AddJsonBody(spec.Body);
            }
            
            // Add cookies
            if (spec.Cookies != null && spec.Cookies.Count > 0)
            {
                foreach (var cookie in spec.Cookies)
                {
                    // Use a default domain for cookies to avoid RestSharp validation errors
                    request.AddCookie(cookie.Key, cookie.Value, "/", "localhost");
                }
            }
            
            // Set timeout if specified
            if (spec.Timeout.HasValue)
            {
                request.Timeout = spec.Timeout.Value;
            }
            
            // Execute request
            var response = _restClient.Execute(request);
            
            return new RestSharpApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            throw new ApiExecutionException("Error during RestSharp request execution", ex, spec);
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
        // If it's relative, RestSharp will combine it with the base URL
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }
        
        // For relative URLs, return as-is so RestSharp can combine with base URL
        return url;
    }

    /// <summary>
    /// Converts HttpMethod to RestSharp Method.
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <returns>RestSharp method</returns>
    private Method GetRestSharpMethod(HttpMethod method)
    {
        return method.Method.ToUpper() switch
        {
            "GET" => Method.Get,
            "POST" => Method.Post,
            "PUT" => Method.Put,
            "PATCH" => Method.Patch,
            "DELETE" => Method.Delete,
            _ => throw new NotSupportedException($"HTTP method {method.Method} is not supported")
        };
    }
}
