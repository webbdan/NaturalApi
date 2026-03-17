namespace NaturalApi;

/// <summary>
/// Shared factory for creating ApiContext instances from an endpoint string.
/// Used by ApiResult, ApiResponse&lt;T&gt;, and any other type that needs to chain
/// a new request from an existing result via the For() method.
/// </summary>
internal static class ApiContextFactory
{
    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <param name="executor">HTTP executor for the new context</param>
    /// <returns>An API context for building and executing requests</returns>
    /// <exception cref="ArgumentException">Thrown when endpoint is null or empty</exception>
    internal static IApiContext CreateContext(string endpoint, IHttpExecutor executor)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        var spec = new ApiRequestSpec(
            endpoint,
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);

        return new ApiContext(spec, executor);
    }
}
