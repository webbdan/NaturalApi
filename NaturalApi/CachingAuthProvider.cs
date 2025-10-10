namespace NaturalApi;

/// <summary>
/// Example implementation of IApiAuthProvider that caches tokens and refreshes them when expired.
/// This demonstrates how to implement token caching with automatic refresh.
/// </summary>
public class CachingAuthProvider : IApiAuthProvider
{
    private string? _token;
    private DateTime _expires;

    /// <summary>
    /// Returns a cached token if still valid, otherwise fetches a new token.
    /// </summary>
    /// <param name="username">Optional username for per-user token resolution</param>
    /// <param name="password">Optional password for authentication</param>
    /// <returns>Cached token if valid, or newly fetched token</returns>
    public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
    {
        if (_token == null || DateTime.UtcNow > _expires)
        {
            var newToken = await FetchNewTokenAsync();
            _token = newToken.Token;
            _expires = DateTime.UtcNow.AddMinutes(newToken.ExpiresInMinutes - 1);
        }
        return _token;
    }

    /// <summary>
    /// Fetches a new token from the authentication service.
    /// This is a placeholder implementation - replace with actual token fetching logic.
    /// </summary>
    /// <returns>New token with expiration information</returns>
    private Task<(string Token, int ExpiresInMinutes)> FetchNewTokenAsync()
        => Task.FromResult(("abc123", 30));
}
