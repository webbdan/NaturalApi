namespace NaturalApi;

/// <summary>
/// Handles API response validation and assertion logic.
/// Decoupled from both the DSL and HTTP handling.
/// </summary>
public interface IApiValidator
{
    /// <summary>
    /// Validates the HTTP status code of a response.
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <param name="expected">Expected status code</param>
    /// <exception cref="ApiAssertionException">Thrown when status code doesn't match</exception>
    void ValidateStatus(HttpResponseMessage response, int expected);

    /// <summary>
    /// Validates response headers using a predicate.
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <param name="predicate">Function to validate headers</param>
    /// <exception cref="ApiAssertionException">Thrown when headers don't match predicate</exception>
    void ValidateHeaders(HttpResponseMessage response, Func<IDictionary<string, string>, bool> predicate);

    /// <summary>
    /// Validates response body using a custom validator function.
    /// </summary>
    /// <typeparam name="T">Type to deserialize body to</typeparam>
    /// <param name="rawBody">Raw response body string</param>
    /// <param name="validator">Function to validate the deserialized body</param>
    /// <exception cref="ApiAssertionException">Thrown when body validation fails</exception>
    void ValidateBody<T>(string rawBody, Action<T> validator);
}
