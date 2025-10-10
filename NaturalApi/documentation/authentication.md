# Authentication Guide

> NaturalApi provides flexible authentication options from simple inline tokens to sophisticated auth providers with caching and per-user token resolution.

---

## Table of Contents

- [Inline Authentication](#inline-authentication)
- [Authentication Providers](#authentication-providers)
- [Token Caching](#token-caching)
- [Per-User Authentication](#per-user-authentication)
- [Custom Auth Providers](#custom-auth-providers)
- [Multi-Tenant Authentication](#multi-tenant-authentication)
- [Best Practices](#best-practices)

---

## Inline Authentication

The simplest way to add authentication is inline with each request:

### Bearer Token Authentication

```csharp
// Simple token (automatically adds "Bearer" prefix)
var data = await api.For("/protected")
    .UsingAuth("your-token-here")
    .Get()
    .ShouldReturn<Data>();

// Explicit Bearer token
var data = await api.For("/protected")
    .UsingAuth("Bearer your-token-here")
    .Get()
    .ShouldReturn<Data>();
```

> **📝 Request Building:** Learn about all authentication options in the [Request Building Guide](request-building.md).

### Custom Authentication Schemes

```csharp
// Custom scheme
var data = await api.For("/protected")
    .UsingAuth("CustomScheme your-token")
    .Get()
    .ShouldReturn<Data>();

// API Key authentication
var data = await api.For("/protected")
    .UsingAuth("ApiKey your-api-key")
    .Get()
    .ShouldReturn<Data>();
```

### UsingToken() Shortcut

```csharp
// Shortcut for Bearer tokens
var data = await api.For("/protected")
    .UsingToken("your-token-here")  // Automatically adds "Bearer" prefix
    .Get()
    .ShouldReturn<Data>();
```

---

## Authentication Providers

For more sophisticated authentication, use `IApiAuthProvider` to handle token management automatically:

### Basic Auth Provider

```csharp
public class SimpleAuthProvider : IApiAuthProvider
{
    private readonly string _token;
    
    public SimpleAuthProvider(string token)
    {
        _token = token;
    }
    
    public Task<string?> GetAuthTokenAsync(string? username = null)
    {
        return Task.FromResult<string?>(_token);
    }
}

// Usage
var authProvider = new SimpleAuthProvider("your-token");
var defaults = new DefaultApiDefaults(authProvider: authProvider);
var api = new Api(executor, defaults);

// All requests automatically include authentication
var data = await api.For("/protected")
    .Get()  // No need to call UsingAuth() - it's automatic
    .ShouldReturn<Data>();
```

### Environment-based Auth Provider

```csharp
public class EnvironmentAuthProvider : IApiAuthProvider
{
    public Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var token = Environment.GetEnvironmentVariable("API_TOKEN");
        return Task.FromResult<string?>(token);
    }
}
```

---

## Token Caching

The built-in `CachingAuthProvider` demonstrates token caching with automatic refresh:

```csharp
public class CachingAuthProvider : IApiAuthProvider
{
    private string? _token;
    private DateTime _expires;

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        if (_token == null || DateTime.UtcNow > _expires)
        {
            var newToken = await FetchNewTokenAsync();
            _token = newToken.Token;
            _expires = DateTime.UtcNow.AddMinutes(newToken.ExpiresInMinutes - 1);
        }
        return _token;
    }

    private Task<(string Token, int ExpiresInMinutes)> FetchNewTokenAsync()
        => Task.FromResult(("abc123", 30));
}
```

### Advanced Caching with Refresh

```csharp
public class AdvancedCachingAuthProvider : IApiAuthProvider
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _token;
    private DateTime _expires;
    private readonly IHttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AdvancedCachingAuthProvider(IHttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_token == null || DateTime.UtcNow > _expires)
            {
                await RefreshTokenAsync();
            }
            return _token;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshTokenAsync()
    {
        var clientId = _configuration["Auth:ClientId"];
        var clientSecret = _configuration["Auth:ClientSecret"];
        
        var response = await _httpClient.PostAsync("https://auth.example.com/token", 
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            }));

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        _token = tokenResponse.AccessToken;
        _expires = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 1 minute early
    }
}
```

---

## Per-User Authentication

Use the `AsUser()` method to specify which user context to authenticate as:

```csharp
// Set user context for this request
var userData = await api.For("/users/me")
    .AsUser("john.doe")  // Passes "john.doe" to auth provider
    .Get()
    .ShouldReturn<UserData>();
```

### Multi-User Auth Provider

```csharp
public class MultiUserAuthProvider : IApiAuthProvider
{
    private readonly Dictionary<string, string> _userTokens = new();
    private readonly IUserTokenService _tokenService;

    public MultiUserAuthProvider(IUserTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        if (string.IsNullOrEmpty(username))
            return null;

        // Check if we have a cached token for this user
        if (_userTokens.TryGetValue(username, out var cachedToken))
        {
            return cachedToken;
        }

        // Fetch token for this specific user
        var token = await _tokenService.GetTokenForUserAsync(username);
        _userTokens[username] = token;
        return token;
    }
}
```

---

## Custom Auth Providers

### OAuth 2.0 Client Credentials

```csharp
public class OAuth2ClientCredentialsProvider : IApiAuthProvider
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenEndpoint;
    private readonly IHttpClient _httpClient;
    private string? _accessToken;
    private DateTime _expiresAt;

    public OAuth2ClientCredentialsProvider(
        string clientId, 
        string clientSecret, 
        string tokenEndpoint,
        IHttpClient httpClient)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
        _httpClient = httpClient;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        if (_accessToken == null || DateTime.UtcNow >= _expiresAt)
        {
            await RefreshTokenAsync();
        }
        return _accessToken;
    }

    private async Task RefreshTokenAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            })
        };

        var response = await _httpClient.SendAsync(request);
        var tokenResponse = await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>();
        
        _accessToken = tokenResponse.AccessToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
    }
}
```

### JWT Token Provider

```csharp
public class JwtAuthProvider : IApiAuthProvider
{
    private readonly IJwtTokenService _jwtService;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtAuthProvider(IJwtTokenService jwtService, string issuer, string audience)
    {
        _jwtService = jwtService;
        _issuer = issuer;
        _audience = audience;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = username ?? "system",
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        return await _jwtService.GenerateTokenAsync(claims);
    }
}
```

---

## Multi-Tenant Authentication

For multi-tenant scenarios where different tenants have different authentication:

```csharp
public class MultiTenantAuthProvider : IApiAuthProvider
{
    private readonly Dictionary<string, IApiAuthProvider> _tenantProviders;
    private readonly ITenantResolver _tenantResolver;

    public MultiTenantAuthProvider(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
        _tenantProviders = new Dictionary<string, IApiAuthProvider>();
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var tenant = await _tenantResolver.GetCurrentTenantAsync();
        
        if (!_tenantProviders.TryGetValue(tenant.Id, out var provider))
        {
            provider = CreateTenantProvider(tenant);
            _tenantProviders[tenant.Id] = provider;
        }

        return await provider.GetAuthTokenAsync(username);
    }

    private IApiAuthProvider CreateTenantProvider(Tenant tenant)
    {
        return new OAuth2ClientCredentialsProvider(
            tenant.ClientId,
            tenant.ClientSecret,
            tenant.TokenEndpoint,
            new HttpClient()
        );
    }
}
```

---

## Disabling Authentication

Use `WithoutAuth()` to disable authentication for specific requests:

```csharp
// Disable auth for this request (overrides default auth provider)
var publicData = await api.For("/public/data")
    .WithoutAuth()  // No authentication header will be added
    .Get()
    .ShouldReturn<PublicData>();
```

---

## Dependency Injection Setup

### Basic DI Registration

```csharp
// Register auth provider
services.AddSingleton<IApiAuthProvider, MyAuthProvider>();

// Register API with auth
services.AddNaturalApiWithAuth<MyAuthProvider>(new MyAuthProvider());
```

> **🔧 DI Guide:** For comprehensive dependency injection patterns, see the [Dependency Injection Guide](di.md) for advanced scenarios and ServiceCollectionExtensions.

### With Configuration

```csharp
services.AddSingleton<IApiAuthProvider>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new OAuth2ClientCredentialsProvider(
        configuration["Auth:ClientId"],
        configuration["Auth:ClientSecret"],
        configuration["Auth:TokenEndpoint"],
        provider.GetRequiredService<HttpClient>()
    );
});

services.AddNaturalApi();
```

### Scoped Auth Provider

```csharp
// For user-specific authentication
services.AddScoped<IApiAuthProvider, UserScopedAuthProvider>();

services.AddScoped<IApi>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var authProvider = provider.GetRequiredService<IApiAuthProvider>();
    var defaults = new DefaultApiDefaults(authProvider: authProvider);
    return new Api(new HttpClientExecutor(httpClient), defaults);
});
```

---

## Testing Authentication

### Unit Testing with Mocks

```csharp
[TestClass]
public class AuthenticationTests
{
    private MockHttpExecutor _mockExecutor;
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _api = new Api(_mockExecutor);
    }
```

> **🧪 Testing Guide:** For comprehensive testing patterns, see the [Testing Guide](testing-guide.md) for unit testing with mocks and integration testing.

    [TestMethod]
    public async Task Should_Add_Authorization_Header_When_UsingAuth()
    {
        // Arrange
        _mockExecutor.SetupResponse(200, """{"message":"success"}""");

        // Act
        await _api.For("/protected")
            .UsingAuth("Bearer test-token")
            .Get()
            .ShouldReturn<object>();

        // Assert
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer test-token", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public async Task Should_Use_Auth_Provider_When_Configured()
    {
        // Arrange
        var authProvider = new Mock<IApiAuthProvider>();
        authProvider.Setup(x => x.GetAuthTokenAsync(null))
            .ReturnsAsync("provider-token");

        var defaults = new DefaultApiDefaults(authProvider: authProvider.Object);
        var api = new Api(_mockExecutor, defaults);

        _mockExecutor.SetupResponse(200, """{"message":"success"}""");

        // Act
        await api.For("/protected")
            .Get()
            .ShouldReturn<object>();

        // Assert
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer provider-token", _mockExecutor.LastSpec.Headers["Authorization"]);
    }
}
```

### Integration Testing

```csharp
[TestClass]
public class AuthenticationIntegrationTests
{
    [TestMethod]
    public async Task Should_Authenticate_With_Real_API()
    {
        // Arrange
        var api = new Api("https://httpbin.org");
        
        // Act & Assert
        var response = await api.For("/headers")
            .UsingAuth("Bearer test-token")
            .Get()
            .ShouldReturn<HeadersResponse>();

        // Verify the Authorization header was sent
        Assert.IsTrue(response.Headers.Authorization.Contains("test-token"));
    }
}
```

---

## Best Practices

### 1. Use Environment Variables for Secrets

```csharp
public class SecureAuthProvider : IApiAuthProvider
{
    public Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var token = Environment.GetEnvironmentVariable("API_TOKEN");
        return Task.FromResult<string?>(token);
    }
}
```

### 2. Implement Proper Token Refresh

```csharp
public class RefreshableAuthProvider : IApiAuthProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _token;
    private DateTime _expiresAt;

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        if (_token == null || DateTime.UtcNow >= _expiresAt)
        {
            await _refreshLock.WaitAsync();
            try
            {
                if (_token == null || DateTime.UtcNow >= _expiresAt)
                {
                    await RefreshTokenAsync();
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }
        return _token;
    }
}
```

### 3. Handle Authentication Failures

```csharp
public class ResilientAuthProvider : IApiAuthProvider
{
    private readonly IApiAuthProvider _primaryProvider;
    private readonly IApiAuthProvider _fallbackProvider;

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        try
        {
            return await _primaryProvider.GetAuthTokenAsync(username);
        }
        catch (Exception)
        {
            // Fallback to secondary auth provider
            return await _fallbackProvider.GetAuthTokenAsync(username);
        }
    }
}
```

### 4. Use Scoped Lifetime for User Context

```csharp
// For user-specific authentication
services.AddScoped<IApiAuthProvider, UserContextAuthProvider>();

// The auth provider will have access to the current user context
public class UserContextAuthProvider : IApiAuthProvider
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokenService;

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var user = _currentUser.GetCurrentUser();
        return await _tokenService.GetTokenForUserAsync(user.Id);
    }
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic authentication setup
- **[Configuration](configuration.md)** - Setting up authentication in DI
- **[Request Building](request-building.md)** - Per-request authentication options
- **[Testing Guide](testing-guide.md)** - Testing authenticated endpoints
- **[Examples](examples.md)** - Real-world authentication scenarios
- **[Troubleshooting](troubleshooting.md)** - Common authentication issues
- **[Extensibility](extensibility.md)** - Creating custom auth providers
- **[API Reference](api-reference.md)** - Complete IApiAuthProvider documentation
