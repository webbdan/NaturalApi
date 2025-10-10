namespace NaturalApi;

/// <summary>
/// Provides default configuration for API requests including base URI, headers, timeout, and auth provider.
/// </summary>
public interface IApiDefaultsProvider
{
    /// <summary>
    /// Base URI for all requests. Can be null if not specified.
    /// </summary>
    Uri? BaseUri { get; }

    /// <summary>
    /// Default headers to be added to all requests.
    /// </summary>
    IDictionary<string, string> DefaultHeaders { get; }

    /// <summary>
    /// Default timeout for requests.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    /// Authentication provider for automatic token resolution.
    /// Can be null if no authentication is configured.
    /// </summary>
    IApiAuthProvider? AuthProvider { get; }
}
