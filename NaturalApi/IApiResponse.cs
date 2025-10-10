namespace NaturalApi;

/// <summary>
/// Represents a generic API response with typed body access.
/// </summary>
/// <typeparam name="T">Type of the response body</typeparam>
public interface IApiResponse<T> : IApiResult
{
    /// <summary>
    /// Gets the deserialized response body.
    /// </summary>
    new T Body { get; }
}
