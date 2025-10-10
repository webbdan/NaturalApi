namespace NaturalApi;

/// <summary>
/// Contract for all authentication providers.
/// Provides tokens to be automatically added to outgoing requests.
/// </summary>
public interface IApiAuthProvider
{
    /// <summary>
    /// Returns a valid auth token (without the scheme).
    /// Returning null means no auth header will be added.
    /// </summary>
    /// <param name="username">Optional username for per-user token resolution</param>
    /// <param name="password">Optional password for authentication</param>
    /// <returns>Authentication token without scheme, or null if no auth should be added</returns>
    Task<string?> GetAuthTokenAsync(string? username = null, string? password = null);
}
