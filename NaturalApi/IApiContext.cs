namespace NaturalApi;

/// <summary>
/// Represents the builder state before execution.
/// Holds all configuration details and composes new contexts fluently.
/// </summary>
public interface IApiContext
{
    /// <summary>
    /// Adds a single HTTP header to the request.
    /// </summary>
    /// <param name="key">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>New context with the header added</returns>
    IApiContext WithHeader(string key, string value);

    /// <summary>
    /// Adds multiple HTTP headers to the request.
    /// </summary>
    /// <param name="headers">Dictionary of headers to add</param>
    /// <returns>New context with headers added</returns>
    IApiContext WithHeaders(IDictionary<string, string> headers);

    /// <summary>
    /// Adds a single query parameter to the request.
    /// </summary>
    /// <param name="key">Parameter name</param>
    /// <param name="value">Parameter value</param>
    /// <returns>New context with the query parameter added</returns>
    IApiContext WithQueryParam(string key, object value);

    /// <summary>
    /// Adds multiple query parameters to the request.
    /// </summary>
    /// <param name="parameters">Object or dictionary containing parameters</param>
    /// <returns>New context with query parameters added</returns>
    IApiContext WithQueryParams(object parameters);

    /// <summary>
    /// Replaces a path parameter in the endpoint URL.
    /// </summary>
    /// <param name="key">Parameter name (without braces)</param>
    /// <param name="value">Parameter value</param>
    /// <returns>New context with path parameter replaced</returns>
    IApiContext WithPathParam(string key, object value);

    /// <summary>
    /// Replaces multiple path parameters in the endpoint URL.
    /// </summary>
    /// <param name="parameters">Object or dictionary containing path parameters</param>
    /// <returns>New context with path parameters replaced</returns>
    IApiContext WithPathParams(object parameters);

    /// <summary>
    /// Adds authentication to the request using a scheme or token.
    /// </summary>
    /// <param name="schemeOrToken">Authentication scheme (e.g., "Bearer") or token</param>
    /// <returns>New context with authentication added</returns>
    IApiContext UsingAuth(string schemeOrToken);

    /// <summary>
    /// Adds Bearer token authentication to the request.
    /// </summary>
    /// <param name="token">Bearer token</param>
    /// <returns>New context with Bearer token authentication</returns>
    IApiContext UsingToken(string token);

    /// <summary>
    /// Disables authentication for this request.
    /// Overrides any default authentication provider.
    /// </summary>
    /// <returns>New context without authentication</returns>
    IApiContext WithoutAuth();

    /// <summary>
    /// Sets the username context for per-user authentication.
    /// This username will be passed to the auth provider for token resolution.
    /// </summary>
    /// <param name="username">Username for authentication context</param>
    /// <returns>New context with username set</returns>
    IApiContext AsUser(string username);

    /// <summary>
    /// Sets a timeout for the request.
    /// </summary>
    /// <param name="timeout">Request timeout</param>
    /// <returns>New context with timeout set</returns>
    IApiContext WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Adds a cookie to the request.
    /// </summary>
    /// <param name="name">Cookie name</param>
    /// <param name="value">Cookie value</param>
    /// <returns>New context with cookie added</returns>
    IApiContext WithCookie(string name, string value);

    /// <summary>
    /// Adds multiple cookies to the request.
    /// </summary>
    /// <param name="cookies">Dictionary of cookies to add</param>
    /// <returns>New context with cookies added</returns>
    IApiContext WithCookies(IDictionary<string, string> cookies);

    /// <summary>
    /// Clears all cookies from the request.
    /// </summary>
    /// <returns>New context with cookies cleared</returns>
    IApiContext ClearCookies();

    /// <summary>
    /// Executes a GET request.
    /// </summary>
    /// <returns>Result context for validation</returns>
    IApiResultContext Get();

    /// <summary>
    /// Executes a DELETE request.
    /// </summary>
    /// <returns>Result context for validation</returns>
    IApiResultContext Delete();

    /// <summary>
    /// Executes a POST request with optional body.
    /// </summary>
    /// <param name="body">Request body (optional)</param>
    /// <returns>Result context for validation</returns>
    IApiResultContext Post(object? body = null);

    /// <summary>
    /// Executes a PUT request with optional body.
    /// </summary>
    /// <param name="body">Request body (optional)</param>
    /// <returns>Result context for validation</returns>
    IApiResultContext Put(object? body = null);

    /// <summary>
    /// Executes a PATCH request with optional body.
    /// </summary>
    /// <param name="body">Request body (optional)</param>
    /// <returns>Result context for validation</returns>
    IApiResultContext Patch(object? body = null);
}
