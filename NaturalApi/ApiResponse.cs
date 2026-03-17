namespace NaturalApi;

/// <summary>
/// Generic wrapper for API responses that includes body, status, headers, and metadata.
/// </summary>
/// <typeparam name="T">Type of the response body</typeparam>
public class ApiResponse<T> : IApiResponse<T>
{
    private readonly IApiResultContext _context;
    private readonly IHttpExecutor _httpExecutor;

    /// <summary>
    /// Initializes a new instance of the ApiResponse class.
    /// </summary>
    /// <param name="context">The API result context</param>
    /// <param name="httpExecutor">HTTP executor for chaining requests</param>
    public ApiResponse(IApiResultContext context, IHttpExecutor httpExecutor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
    }

    /// <summary>
    /// Gets the deserialized response body.
    /// </summary>
    public T Body => _context.BodyAs<T>();

    /// <summary>
    /// Gets the deserialized response body as dynamic (from IApiResult).
    /// </summary>
    dynamic IApiResult.Body => _context.BodyAs<T>();

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
    /// <typeparam name="TBody">Type to deserialize to</typeparam>
    /// <returns>Deserialized object</returns>
    public TBody BodyAs<TBody>() => _context.BodyAs<TBody>();

    /// <summary>
    /// Validates the response and returns the deserialized object.
    /// </summary>
    /// <typeparam name="TBody">Expected response type</typeparam>
    /// <returns>The deserialized response object</returns>
    public TBody ShouldReturn<TBody>() => _context.ShouldReturn<TBody>();

    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// Allows chaining from a result to make another API call.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <returns>An API context for building and executing requests</returns>
    public IApiContext For(string endpoint) => ApiContextFactory.CreateContext(endpoint, _httpExecutor);
}
