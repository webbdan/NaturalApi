namespace NaturalApi;

/// <summary>
/// Immutable record storing the evolving request state.
/// Contains URL, headers, parameters, body, and other request details.
/// </summary>
/// <param name="Endpoint">The target endpoint URL</param>
/// <param name="Method">HTTP method to use</param>
/// <param name="Headers">Request headers</param>
/// <param name="QueryParams">Query parameters</param>
/// <param name="PathParams">Path parameters for URL replacement</param>
/// <param name="Body">Request body</param>
/// <param name="Timeout">Request timeout</param>
/// <param name="SuppressAuth">Whether to suppress authentication for this request</param>
/// <param name="Username">Username context for per-user authentication</param>
/// <param name="Password">Password context for authentication</param>
/// <param name="Cookies">Request cookies to be sent with the request</param>
public record ApiRequestSpec(
    string Endpoint,
    HttpMethod Method,
    IDictionary<string, string> Headers,
    IDictionary<string, object> QueryParams,
    IDictionary<string, object> PathParams,
    object? Body,
    TimeSpan? Timeout,
    bool SuppressAuth = false,
    string? Username = null,
    string? Password = null,
    IDictionary<string, string> Cookies = null)
{
    /// <summary>
    /// Creates a new specification with an additional header.
    /// </summary>
    /// <param name="key">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>New specification with the header added</returns>
    public ApiRequestSpec WithHeader(string key, string value)
    {
        var newHeaders = new Dictionary<string, string>(Headers) { [key] = value };
        return this with { Headers = newHeaders };
    }

    /// <summary>
    /// Creates a new specification with additional headers.
    /// </summary>
    /// <param name="headers">Headers to add</param>
    /// <returns>New specification with headers added</returns>
    public ApiRequestSpec WithHeaders(IDictionary<string, string> headers)
    {
        var newHeaders = new Dictionary<string, string>(Headers);
        foreach (var header in headers)
        {
            newHeaders[header.Key] = header.Value;
        }
        return this with { Headers = newHeaders };
    }

    /// <summary>
    /// Creates a new specification with an additional query parameter.
    /// </summary>
    /// <param name="key">Parameter name</param>
    /// <param name="value">Parameter value</param>
    /// <returns>New specification with the query parameter added</returns>
    public ApiRequestSpec WithQueryParam(string key, object value)
    {
        var newQueryParams = new Dictionary<string, object>(QueryParams) { [key] = value };
        return this with { QueryParams = newQueryParams };
    }

    /// <summary>
    /// Creates a new specification with additional query parameters.
    /// </summary>
    /// <param name="parameters">Parameters to add</param>
    /// <returns>New specification with query parameters added</returns>
    public ApiRequestSpec WithQueryParams(object parameters)
    {
        var newQueryParams = new Dictionary<string, object>(QueryParams);
        
        if (parameters is IDictionary<string, object> dict)
        {
            foreach (var param in dict)
            {
                newQueryParams[param.Key] = param.Value;
            }
        }
        else
        {
            // Use reflection to extract properties
            var properties = parameters.GetType().GetProperties();
            foreach (var prop in properties)
            {
                newQueryParams[prop.Name] = prop.GetValue(parameters) ?? string.Empty;
            }
        }
        
        return this with { QueryParams = newQueryParams };
    }

    /// <summary>
    /// Creates a new specification with an additional path parameter.
    /// </summary>
    /// <param name="key">Parameter name</param>
    /// <param name="value">Parameter value</param>
    /// <returns>New specification with the path parameter added</returns>
    public ApiRequestSpec WithPathParam(string key, object value)
    {
        var newPathParams = new Dictionary<string, object>(PathParams) { [key] = value };
        return this with { PathParams = newPathParams };
    }

    /// <summary>
    /// Creates a new specification with additional path parameters.
    /// </summary>
    /// <param name="parameters">Parameters to add</param>
    /// <returns>New specification with path parameters added</returns>
    public ApiRequestSpec WithPathParams(object parameters)
    {
        var newPathParams = new Dictionary<string, object>(PathParams);
        
        if (parameters is IDictionary<string, object> dict)
        {
            foreach (var param in dict)
            {
                newPathParams[param.Key] = param.Value;
            }
        }
        else
        {
            // Use reflection to extract properties
            var properties = parameters.GetType().GetProperties();
            foreach (var prop in properties)
            {
                newPathParams[prop.Name] = prop.GetValue(parameters) ?? string.Empty;
            }
        }
        
        return this with { PathParams = newPathParams };
    }

    /// <summary>
    /// Creates a new specification with a different HTTP method.
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <returns>New specification with the method set</returns>
    public ApiRequestSpec WithMethod(HttpMethod method)
    {
        return this with { Method = method };
    }

    /// <summary>
    /// Creates a new specification with a request body.
    /// </summary>
    /// <param name="body">Request body</param>
    /// <returns>New specification with the body set</returns>
    public ApiRequestSpec WithBody(object? body)
    {
        return this with { Body = body };
    }

    /// <summary>
    /// Creates a new specification with a timeout.
    /// </summary>
    /// <param name="timeout">Request timeout</param>
    /// <returns>New specification with the timeout set</returns>
    public ApiRequestSpec WithTimeout(TimeSpan timeout)
    {
        return this with { Timeout = timeout };
    }

    /// <summary>
    /// Creates a new specification with authentication suppressed.
    /// </summary>
    /// <returns>New specification with authentication suppressed</returns>
    public ApiRequestSpec WithoutAuth()
    {
        return this with { SuppressAuth = true };
    }

    /// <summary>
    /// Creates a new specification with a username context for per-user authentication.
    /// </summary>
    /// <param name="username">Username for authentication context</param>
    /// <returns>New specification with username set</returns>
    public ApiRequestSpec AsUser(string username)
    {
        return this with { Username = username };
    }

    /// <summary>
    /// Creates a new specification with username and password context for authentication.
    /// </summary>
    /// <param name="username">Username for authentication context</param>
    /// <param name="password">Password for authentication</param>
    /// <returns>New specification with username and password set</returns>
    public ApiRequestSpec AsUser(string username, string password)
    {
        return this with { Username = username, Password = password };
    }

    /// <summary>
    /// Creates a new specification with an additional cookie.
    /// </summary>
    /// <param name="name">Cookie name</param>
    /// <param name="value">Cookie value</param>
    /// <returns>New specification with the cookie added</returns>
    public ApiRequestSpec WithCookie(string name, string value)
    {
        var newCookies = new Dictionary<string, string>(Cookies ?? new Dictionary<string, string>()) { [name] = value };
        return this with { Cookies = newCookies };
    }

    /// <summary>
    /// Creates a new specification with additional cookies.
    /// </summary>
    /// <param name="cookies">Cookies to add</param>
    /// <returns>New specification with cookies added</returns>
    public ApiRequestSpec WithCookies(IDictionary<string, string> cookies)
    {
        var newCookies = new Dictionary<string, string>(Cookies ?? new Dictionary<string, string>());
        foreach (var cookie in cookies)
        {
            newCookies[cookie.Key] = cookie.Value;
        }
        return this with { Cookies = newCookies };
    }

    /// <summary>
    /// Creates a new specification with cookies cleared.
    /// </summary>
    /// <returns>New specification with no cookies</returns>
    public ApiRequestSpec ClearCookies()
    {
        return this with { Cookies = new Dictionary<string, string>() };
    }
}
