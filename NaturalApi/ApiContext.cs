namespace NaturalApi;

/// <summary>
/// Immutable context for building API requests.
/// Each method returns a new context with accumulated state.
/// </summary>
public sealed class ApiContext : IApiContext
{
    private readonly ApiRequestSpec _spec;
    private readonly IHttpExecutor _executor;

    /// <summary>
    /// Initializes a new instance of the ApiContext class.
    /// </summary>
    /// <param name="spec">Request specification</param>
    /// <param name="executor">HTTP executor</param>
    public ApiContext(ApiRequestSpec spec, IHttpExecutor executor)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// Adds a single HTTP header to the request.
    /// </summary>
    public IApiContext WithHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be null or empty", nameof(key));

        var newSpec = _spec.WithHeader(key, value);
        return new ApiContext(newSpec, _executor);
    }

    /// <summary>
    /// Adds multiple HTTP headers to the request.
    /// </summary>
    public IApiContext WithHeaders(IDictionary<string, string> headers)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        var newSpec = _spec.WithHeaders(headers);
        return new ApiContext(newSpec, _executor);
    }

    /// <summary>
    /// Adds a single query parameter to the request.
    /// </summary>
    public IApiContext WithQueryParam(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Query parameter key cannot be null or empty", nameof(key));

        var newSpec = _spec.WithQueryParam(key, value);
        return new ApiContext(newSpec, _executor);
    }

    /// <summary>
    /// Adds multiple query parameters to the request.
    /// </summary>
    public IApiContext WithQueryParams(object parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var newSpec = _spec.WithQueryParams(parameters);
        return new ApiContext(newSpec, _executor);
    }

    /// <summary>
    /// Replaces a path parameter in the endpoint URL.
    /// </summary>
    public IApiContext WithPathParam(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Path parameter key cannot be null or empty", nameof(key));

        var newSpec = _spec.WithPathParam(key, value);
        return new ApiContext(newSpec, _executor);
    }

    /// <summary>
    /// Replaces multiple path parameters in the endpoint URL.
    /// </summary>
    public IApiContext WithPathParams(object parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var newSpec = _spec.WithPathParams(parameters);
        return new ApiContext(newSpec, _executor);
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
    /// Sets a timeout for the request.
    /// </summary>
    public IApiContext WithTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be positive", nameof(timeout));

        var newSpec = _spec.WithTimeout(timeout);
        return new ApiContext(newSpec, _executor);
    }

    /// <summary>
    /// Executes a GET request.
    /// </summary>
    public IApiResultContext Get()
    {
        var spec = _spec.WithMethod(HttpMethod.Get);
        return _executor.Execute(spec);
    }

    /// <summary>
    /// Executes a DELETE request.
    /// </summary>
    public IApiResultContext Delete()
    {
        var spec = _spec.WithMethod(HttpMethod.Delete);
        return _executor.Execute(spec);
    }

    /// <summary>
    /// Executes a POST request with optional body.
    /// </summary>
    public IApiResultContext Post(object? body = null)
    {
        var spec = _spec.WithMethod(HttpMethod.Post).WithBody(body);
        return _executor.Execute(spec);
    }

    /// <summary>
    /// Executes a PUT request with optional body.
    /// </summary>
    public IApiResultContext Put(object? body = null)
    {
        var spec = _spec.WithMethod(HttpMethod.Put).WithBody(body);
        return _executor.Execute(spec);
    }

    /// <summary>
    /// Executes a PATCH request with optional body.
    /// </summary>
    public IApiResultContext Patch(object? body = null)
    {
        var spec = _spec.WithMethod(HttpMethod.Patch).WithBody(body);
        return _executor.Execute(spec);
    }
}
