using NaturalApi;

namespace NaturalApi.Integration.Tests.Complex;

/// <summary>
/// NaturalApi auth provider that uses username/password authentication with token caching.
/// </summary>
public class CustomAuthProvider : IApiAuthProvider
{
    private readonly IUsernamePasswordAuthService _authService;
    private readonly string _username;
    private readonly string _password;

    public CustomAuthProvider(
        IUsernamePasswordAuthService authService, 
        string username, 
        string password)
    {
        _authService = authService;
        _username = username;
        _password = password;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
    {
        // Use the provided credentials or fall back to defaults
        var targetUsername = username ?? _username;
        var targetPassword = password ?? _password;

        // First, try to get a cached token
        var cachedToken = await _authService.GetCachedTokenAsync(targetUsername);
        if (cachedToken != null)
        {
            return cachedToken;
        }

        // No cached token or expired, authenticate to get a new one
        return await _authService.AuthenticateAsync(targetUsername, targetPassword);
    }
}
