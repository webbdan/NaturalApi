namespace NaturalApi;

/// <summary>
/// The root entry point for the NaturalApi fluent DSL.
/// Exposes the For() method to create API contexts.
/// </summary>
public interface IApi
{
    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <returns>An API context for building and executing requests</returns>
    IApiContext For(string endpoint);
}
