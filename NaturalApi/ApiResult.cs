namespace NaturalApi;

/// <summary>
/// Implementation of IApiResult that provides access to deserialized response data.
/// </summary>
public class ApiResult : IApiResult
{
    private readonly IApiResultContext _context;
    private readonly IHttpExecutor _httpExecutor;

    /// <summary>
    /// Initializes a new instance of the ApiResult class.
    /// </summary>
    /// <param name="context">The API result context</param>
    /// <param name="httpExecutor">HTTP executor for chaining requests</param>
    public ApiResult(IApiResultContext context, IHttpExecutor httpExecutor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
    }

    /// <summary>
    /// Gets the deserialized response body.
    /// </summary>
    public dynamic Body => _context.BodyAs<dynamic>();

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => _context.StatusCode;

    /// <summary>
    /// Gets the response headers as a dictionary.
    /// </summary>
    public IDictionary<string, string> Headers => _context.Headers;

    /// <summary>
    /// Gets the raw response body as a string.
    /// </summary>
    public string RawBody => _context.RawBody;

    /// <summary>
    /// Gets the raw HTTP response message.
    /// </summary>
    public HttpResponseMessage Response => _context.Response;

    /// <summary>
    /// Deserializes the response body into the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns>Deserialized object</returns>
    public T BodyAs<T>() => _context.BodyAs<T>();

    /// <summary>
    /// Validates the response and returns the deserialized object.
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <returns>The deserialized response object</returns>
    public T ShouldReturn<T>() => _context.ShouldReturn<T>();

    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// Allows chaining from a result to make another API call.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <returns>An API context for building and executing requests</returns>
    public IApiContext For(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        var spec = new ApiRequestSpec(
            endpoint,
            HttpMethod.Get, // Default method, will be overridden by verb methods
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);

        return new ApiContext(spec, _httpExecutor);
    }
}
