using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NaturalApi.Integration.Tests.Complex;

/// <summary>
/// Interface for username/password authentication service.
/// </summary>
public interface IUsernamePasswordAuthService
{
    /// <summary>
    /// Authenticates a user and returns a token.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <returns>The authentication token if valid, null if invalid</returns>
    Task<string?> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// Gets a cached token for a user if it exists and is not expired.
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The cached token if valid, null if not cached or expired</returns>
    Task<string?> GetCachedTokenAsync(string username);
}

/// <summary>
/// Authentication service that handles username/password authentication with token caching.
/// </summary>
public class UsernamePasswordAuthService : IUsernamePasswordAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CachedToken> _tokenCache;
    private readonly TimeSpan _cacheExpiration;

    public UsernamePasswordAuthService(HttpClient httpClient, TimeSpan? cacheExpiration = null)
    {
        _httpClient = httpClient;
        _tokenCache = new ConcurrentDictionary<string, CachedToken>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(10);
    }

    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        try
        {
            var loginRequest = new
            {
                username = username,
                password = password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent);
                
                if (authResponse?.Token != null)
                {
                    // Cache the token
                    var cachedToken = new CachedToken
                    {
                        Token = authResponse.Token,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn)
                    };
                    
                    _tokenCache.AddOrUpdate(username, cachedToken, (key, oldValue) => cachedToken);
                    return authResponse.Token;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public Task<string?> GetCachedTokenAsync(string username)
    {
        if (_tokenCache.TryGetValue(username, out var cachedToken))
        {
            if (DateTime.UtcNow < cachedToken.ExpiresAt)
            {
                return Task.FromResult<string?>(cachedToken.Token);
            }
            else
            {
                // Token expired, remove from cache
                _tokenCache.TryRemove(username, out _);
            }
        }
        
        return Task.FromResult<string?>(null);
    }

    private class CachedToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class AuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }
}
