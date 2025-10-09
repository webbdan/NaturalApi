namespace NaturalApi;

/// <summary>
/// Exception thrown when API assertions fail.
/// Contains structured information about the failed expectation.
/// </summary>
public class ApiAssertionException : Exception
{
    /// <summary>
    /// Gets the failed expectation description.
    /// </summary>
    public string FailedExpectation { get; }

    /// <summary>
    /// Gets the actual values that caused the failure.
    /// </summary>
    public string ActualValues { get; }

    /// <summary>
    /// Gets the endpoint that was called.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the HTTP verb that was used.
    /// </summary>
    public string HttpVerb { get; }

    /// <summary>
    /// Gets an optional snippet of the response body.
    /// </summary>
    public string? ResponseBodySnippet { get; }

    /// <summary>
    /// Initializes a new instance of the ApiAssertionException class.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="failedExpectation">Description of what was expected</param>
    /// <param name="actualValues">Description of what was actually received</param>
    /// <param name="endpoint">Endpoint that was called</param>
    /// <param name="httpVerb">HTTP verb that was used</param>
    /// <param name="responseBodySnippet">Optional response body snippet</param>
    public ApiAssertionException(
        string message,
        string failedExpectation,
        string actualValues,
        string endpoint,
        string httpVerb,
        string? responseBodySnippet = null)
        : base(message)
    {
        FailedExpectation = failedExpectation;
        ActualValues = actualValues;
        Endpoint = endpoint;
        HttpVerb = httpVerb;
        ResponseBodySnippet = responseBodySnippet;
    }
}
