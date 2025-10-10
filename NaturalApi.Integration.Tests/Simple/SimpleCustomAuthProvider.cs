using NaturalApi;

namespace NaturalApi.Integration.Tests.Simple;

/// <summary>
/// Simple auth provider that demonstrates the "just works" approach.
/// This provider accepts username and password directly from the request context.
/// </summary>
public class SimpleCustomAuthProvider : IApiAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _authEndpoint;

    public SimpleCustomAuthProvider(HttpClient httpClient, string authEndpoint)
    {
        _httpClient = httpClient;
        _authEndpoint = authEndpoint;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
    {
        // If no credentials provided, return null (no auth)
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        try
        {
            // Simple authentication - call the auth service
            var loginRequest = new
            {
                username = username,
                password = password
            };

            var json = System.Text.Json.JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_authEndpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(responseContent);
                return authResponse?.Token;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private class AuthResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }
}
