namespace NaturalApi;

/// <summary>
/// Represents the executed API call, exposing both the raw response and fluent validation methods.
/// </summary>
public interface IApiResultContext
{
    /// <summary>
    /// Gets the raw HTTP response message.
    /// </summary>
    HttpResponseMessage Response { get; }

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
    /// Deserializes the response body into the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns>Deserialized object</returns>
    T BodyAs<T>();

    /// <summary>
    /// Validates the response using fluent assertions.
    /// </summary>
    /// <typeparam name="T">Expected response body type</typeparam>
    /// <param name="status">Expected status code (optional)</param>
    /// <param name="bodyValidator">Body validation function (optional)</param>
    /// <param name="headers">Header validation function (optional)</param>
    /// <returns>This result context for chaining</returns>
    IApiResultContext ShouldReturn<T>(
        int? status = null,
        Func<T, bool>? bodyValidator = null,
        Func<IDictionary<string, string>, bool>? headers = null);

    /// <summary>
    /// Validates the response status code only.
    /// </summary>
    /// <param name="status">Expected status code</param>
    /// <returns>This result context for chaining</returns>
    IApiResultContext ShouldReturn(int status);

    /// <summary>
    /// Validates the response body only.
    /// </summary>
    /// <typeparam name="T">Expected response body type</typeparam>
    /// <param name="bodyValidator">Body validation function</param>
    /// <returns>This result context for chaining</returns>
    IApiResultContext ShouldReturn<T>(Func<T, bool> bodyValidator);

    /// <summary>
    /// Validates the response status and headers.
    /// </summary>
    /// <param name="status">Expected status code</param>
    /// <param name="headers">Header validation function</param>
    /// <returns>This result context for chaining</returns>
    IApiResultContext ShouldReturn(int status, Func<IDictionary<string, string>, bool> headers);

    /// <summary>
    /// Validates the response and returns the deserialized object.
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <returns>The deserialized response object</returns>
    T ShouldReturn<T>();

    /// <summary>
    /// Allows chaining additional operations or validations.
    /// </summary>
    /// <param name="next">Action to perform on this result</param>
    /// <returns>This result context for chaining</returns>
    IApiResultContext Then(Action<IApiResult> next);
}
