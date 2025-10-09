namespace NaturalApi;

/// <summary>
/// Represents the result of an API call with access to the deserialized body.
/// Used in Then() chaining to provide easy access to response data.
/// </summary>
public interface IApiResult
{
    /// <summary>
    /// Gets the deserialized response body.
    /// </summary>
    dynamic Body { get; }
    
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    int StatusCode { get; }
    
    /// <summary>
    /// Gets the response headers as a dictionary.
    /// </summary>
    IDictionary<string, string> Headers { get; }
    
    /// <summary>
    /// Gets the raw response body as a string.
    /// </summary>
    string RawBody { get; }

    /// <summary>
    /// Gets the raw HTTP response message.
    /// </summary>
    HttpResponseMessage Response { get; }

    /// <summary>
    /// Deserializes the response body into the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns>Deserialized object</returns>
    T BodyAs<T>();

    /// <summary>
    /// Validates the response and returns the deserialized object.
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <returns>The deserialized response object</returns>
    T ShouldReturn<T>();

    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// Allows chaining from a result to make another API call.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <returns>An API context for building and executing requests</returns>
    IApiContext For(string endpoint);
}
