using NaturalApi.Reporter;
using System.Diagnostics;

namespace NaturalApi;

/// <summary>
/// Immutable context for building API requests.
/// Each method returns a new context with accumulated state.
/// </summary>
public sealed class ApiContext : IApiContext
{
    private readonly ApiRequestSpec _spec;
    private readonly IHttpExecutor _executor;
    private readonly IApiAuthProvider? _authProvider;

    /// <summary>
    /// Initializes a new instance of the ApiContext class.
    /// </summary>
    /// <param name="spec">Request specification</param>
    /// <param name="executor">HTTP executor</param>
    public ApiContext(ApiRequestSpec spec, IHttpExecutor executor)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _authProvider = null;
    }

    /// <summary>
    /// Initializes a new instance of the ApiContext class with authentication support.
    /// </summary>
    /// <param name="spec">Request specification</param>
    /// <param name="executor">HTTP executor</param>
    /// <param name="authProvider">Authentication provider</param>
    public ApiContext(ApiRequestSpec spec, IHttpExecutor executor, IApiAuthProvider? authProvider)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _authProvider = authProvider;
        
    }

    /// <summary>
    /// Adds a single HTTP header to the request.
    /// </summary>
    public IApiContext WithHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be null or empty", nameof(key));

        var newSpec = _spec.WithHeader(key, value);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Adds multiple HTTP headers to the request.
    /// </summary>
    public IApiContext WithHeaders(IDictionary<string, string> headers)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        var newSpec = _spec.WithHeaders(headers);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Adds a single query parameter to the request.
    /// </summary>
    public IApiContext WithQueryParam(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Query parameter key cannot be null or empty", nameof(key));

        var newSpec = _spec.WithQueryParam(key, value);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Adds multiple query parameters to the request.
    /// </summary>
    public IApiContext WithQueryParams(object parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var newSpec = _spec.WithQueryParams(parameters);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Replaces a path parameter in the endpoint URL.
    /// </summary>
    public IApiContext WithPathParam(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Path parameter key cannot be null or empty", nameof(key));

        var newSpec = _spec.WithPathParam(key, value);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Replaces multiple path parameters in the endpoint URL.
    /// </summary>
    public IApiContext WithPathParams(object parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var newSpec = _spec.WithPathParams(parameters);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Adds authentication to the request using a scheme or token.
    /// </summary>
    public IApiContext UsingAuth(string schemeOrToken)
    {
        if (string.IsNullOrWhiteSpace(schemeOrToken))
            throw new ArgumentException("Authentication scheme or token cannot be null or empty", nameof(schemeOrToken));

        // If it looks like a token (no space), assume Bearer
        var authValue = schemeOrToken.Contains(' ') ? schemeOrToken : $"Bearer {schemeOrToken}";
        return WithHeader("Authorization", authValue);
    }

    /// <summary>
    /// Adds Bearer token authentication to the request.
    /// </summary>
    public IApiContext UsingToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        return UsingAuth($"Bearer {token}");
    }

    /// <summary>
    /// Disables authentication for this request.
    /// Overrides any default authentication provider.
    /// </summary>
    public IApiContext WithoutAuth()
    {
        var newSpec = _spec.WithoutAuth();
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Sets the username context for per-user authentication.
    /// This username will be passed to the auth provider for token resolution.
    /// </summary>
    public IApiContext AsUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        var newSpec = _spec.AsUser(username);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Sets the username and password context for authentication.
    /// Both credentials will be passed to the auth provider for token resolution.
    /// </summary>
    public IApiContext AsUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var newSpec = _spec.AsUser(username, password);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Sets a timeout for the request.
    /// </summary>
    public IApiContext WithTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be positive", nameof(timeout));

        var newSpec = _spec.WithTimeout(timeout);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Adds a cookie to the request.
    /// </summary>
    public IApiContext WithCookie(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cookie name cannot be null or empty", nameof(name));

        var newSpec = _spec.WithCookie(name, value);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Adds multiple cookies to the request.
    /// </summary>
    public IApiContext WithCookies(IDictionary<string, string> cookies)
    {
        if (cookies == null)
            throw new ArgumentNullException(nameof(cookies));

        var newSpec = _spec.WithCookies(cookies);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Clears all cookies from the request.
    /// </summary>
    public IApiContext ClearCookies()
    {
        var newSpec = _spec.ClearCookies();
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Set a reporter for this request (per-call override).
    /// </summary>
    public IApiContext WithReporter(INaturalReporter reporter)
    {
        if (reporter == null) throw new ArgumentNullException(nameof(reporter));
        var newSpec = _spec.WithReporter(reporter);
        return new ApiContext(newSpec, _executor, _authProvider);
    }

    /// <summary>
    /// Executes a GET request.
    /// </summary>
    public IApiResultContext Get()
    {
        var spec = _spec.WithMethod(HttpMethod.Get);
        return ExecuteWithAuth(spec);
    }

    /// <summary>
    /// Executes a DELETE request.
    /// </summary>
    public IApiResultContext Delete()
    {
        var spec = _spec.WithMethod(HttpMethod.Delete);
        return ExecuteWithAuth(spec);
    }

    /// <summary>
    /// Executes a POST request with optional body.
    /// </summary>
    public IApiResultContext Post(object? body = null)
    {
        var spec = _spec.WithMethod(HttpMethod.Post).WithBody(body);
        return ExecuteWithAuth(spec);
    }

    /// <summary>
    /// Executes a PUT request with optional body.
    /// </summary>
    public IApiResultContext Put(object? body = null)
    {
        var spec = _spec.WithMethod(HttpMethod.Put).WithBody(body);
        return ExecuteWithAuth(spec);
    }

    /// <summary>
    /// Executes a PATCH request with optional body.
    /// </summary>
    public IApiResultContext Patch(object? body = null)
    {
        var spec = _spec.WithMethod(HttpMethod.Patch).WithBody(body);
        return ExecuteWithAuth(spec);
    }

    /// <summary>
    /// Executes the request with authentication support if available.
    /// </summary>
    /// <param name="spec">Request specification</param>
    /// <returns>Result context</returns>
    private IApiResultContext ExecuteWithAuth(ApiRequestSpec spec)
    {
        
        if (_executor is IAuthenticatedHttpExecutor authExecutor && _authProvider != null)
        {
            // Use authenticated executor
            return authExecutor.ExecuteAsync(spec, _authProvider, _spec.Username, _spec.Password, _spec.SuppressAuth).GetAwaiter().GetResult();
        }
        else
        {
            // Use regular executor
            return _executor.Execute(spec);
        }

    }
}
