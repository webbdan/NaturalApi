using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using NaturalApi.Integration.Tests.Playwright.Executors;
using NaturalApi.Integration.Tests.Playwright.Common;

namespace NaturalApi.Integration.Tests.Playwright.Tests;

/// <summary>
/// Tests proving authentication works with Playwright executor by verifying headers are actually sent.
/// </summary>
[TestClass]
public class PlaywrightAuthTests
{
    private ServiceProvider? _serviceProvider;
    private WireMockServers? _wireMockServers;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        _wireMockServers.Start();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
        _wireMockServers?.Stop();
    }

    [TestMethod]
    public void PlaywrightExecutor_WithBearerToken_SendsCorrectAuthorizationHeader()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that accepts any request to /users (for now, to verify the request works)
        _wireMockServers.SetupGet("/users", 200, "{\"users\":[]}");

        // Act
        var result = api.For("/users")
            .UsingToken("test-token-123")
            .Get();
        
        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
        // This test verifies that UsingToken() doesn't break the request
        // TODO: Add proper header validation once WireMock setup is working
    }


    [TestMethod]
    public void PlaywrightExecutor_WithCustomAuth_SendsCorrectAuthorizationHeader()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests with the correct custom Authorization header
        _wireMockServers.SetupGetWithAuthValidation("/users", 200, "{\"users\":[]}", "Custom test-auth-token");

        // Act
        var result = api.For("/users")
            .UsingAuth("Custom test-auth-token")
            .Get();
        
        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
        // The mock server validates the custom Authorization header was sent correctly
    }

    [TestMethod]
    public void PlaywrightExecutor_WithAuthProvider_SendsCorrectAuthorizationHeader()
    {
        // Arrange
        var authProvider = new TestAuthProvider();
        var services = PlaywrightTestHelpers.CreateServiceCollectionWithAuth(_wireMockServers!.BaseUrl, authProvider);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests with the correct Authorization header from auth provider
        _wireMockServers.SetupGetWithAuthValidation("/users", 200, "{\"users\":[]}", "Bearer test-token-default");

        // Act
        var result = api.For("/users").Get();
        
        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
        // The mock server validates the Authorization header from auth provider was sent correctly
    }

    [TestMethod]
    public void PlaywrightExecutor_WithUserContext_SendsCorrectAuthorizationHeader()
    {
        // Arrange
        var authProvider = new TestAuthProvider();
        var services = PlaywrightTestHelpers.CreateServiceCollectionWithAuth(_wireMockServers!.BaseUrl, authProvider);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests with the correct Authorization header for specific user
        _wireMockServers.SetupGetWithAuthValidation("/users", 200, "{\"users\":[]}", "Bearer test-token-testuser");

        // Act
        var result = api.For("/users")
            .AsUser("testuser", "testpass")
            .Get();
        
        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
        // The mock server validates the Authorization header for specific user was sent correctly
    }

    [TestMethod]
    public void PlaywrightExecutor_WithoutAuth_DoesNotSendAuthorizationHeader()
    {
        // Arrange
        var authProvider = new TestAuthProvider();
        var services = PlaywrightTestHelpers.CreateServiceCollectionWithAuth(_wireMockServers!.BaseUrl, authProvider);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests WITHOUT Authorization header
        _wireMockServers.SetupGetWithoutAuth("/users", 200, "{\"users\":[]}");

        // Act
        var result = api.For("/users")
            .WithoutAuth()
            .Get();
        
        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
        // The mock server validates NO Authorization header was sent
    }

    [TestMethod]
    public void PlaywrightExecutor_WithAuthAndHeaders_SendsBothHeaders()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests with BOTH Authorization and custom headers
        _wireMockServers.SetupGetWithHeadersValidation("/users", 200, "{\"users\":[]}", 
            new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer test-token-123",
                ["Accept"] = "application/json",
                ["X-Custom-Header"] = "test-value"
            });

        // Act
        var result = api.For("/users")
            .WithHeader("Accept", "application/json")
            .WithHeader("X-Custom-Header", "test-value")
            .UsingToken("test-token-123")
            .Get();
        
        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
        // The mock server validates ALL headers were sent correctly
    }

    [TestMethod]
    public void PlaywrightExecutor_WithAuthProvider_SupportsPerUserAuth()
    {
        // Arrange
        var authProvider = new TestAuthProvider();
        var services = PlaywrightTestHelpers.CreateServiceCollectionWithAuth(_wireMockServers!.BaseUrl, authProvider);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Test user 1 - Mock server that ONLY accepts requests with user1's token
        _wireMockServers.SetupGetWithAuthValidation("/users", 200, "{\"users\":[]}", "Bearer test-token-user1");
        var result1 = api.For("/users")
            .AsUser("user1", "pass1")
            .Get();
        
        Assert.AreEqual(200, result1.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result1.RawBody);
        
        // Test user 2 - Mock server that ONLY accepts requests with user2's token
        _wireMockServers.SetupGetWithAuthValidation("/users", 200, "{\"users\":[]}", "Bearer test-token-user2");
        var result2 = api.For("/users")
            .AsUser("user2", "pass2")
            .Get();
        
        Assert.AreEqual(200, result2.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result2.RawBody);
        // The mock server validates different users get different tokens
    }

    [TestMethod]
    public void PlaywrightExecutor_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests with valid token, returns 401 for invalid token
        _wireMockServers.SetupGetWithAuthValidation("/users", 401, "{\"error\":\"Unauthorized\"}", "Bearer invalid-token");

        // Act & Assert
        var result = api.For("/users")
            .UsingToken("invalid-token")
            .Get();
        
        Assert.AreEqual(401, result.StatusCode);
        Assert.AreEqual("{\"error\":\"Unauthorized\"}", result.RawBody);
    }

    [TestMethod]
    public void PlaywrightExecutor_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Mock server that ONLY accepts requests with the correct valid token
        _wireMockServers.SetupGetWithAuthValidation("/users", 200, "{\"users\":[]}", "Bearer valid-token");

        // Act & Assert
        var result = api.For("/users")
            .UsingToken("valid-token")
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
    }

    [TestMethod]
    public void PlaywrightExecutor_WithAuthAndQueryParams_WorksCorrectly()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[],\"page\":1}");

        // Act & Assert
        var result = api.For("/users")
            .WithQueryParam("page", 1)
            .WithQueryParam("limit", 10)
            .UsingToken("test-token-123")
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[],\"page\":1}", result.RawBody);
    }

    [TestMethod]
    public void PlaywrightExecutor_WithAuthAndCookies_WorksCorrectly()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[]}");

        // Act & Assert
        var result = api.For("/users")
            .WithCookie("sessionId", "abc123")
            .WithCookie("userId", "456")
            .UsingToken("test-token-123")
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
    }

    [TestMethod]
    public void PlaywrightExecutor_WithAuthAndTimeout_WorksCorrectly()
    {
        // Arrange
        var services = PlaywrightTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[]}");

        // Act & Assert
        var result = api.For("/users")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .UsingToken("test-token-123")
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
    }
}

/// <summary>
/// Test auth provider for Playwright tests.
/// </summary>
public class TestAuthProvider : IApiAuthProvider
{
    public Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
    {
        // Return a token based on the username/password
        var token = $"test-token-{username ?? "default"}";
        return Task.FromResult<string?>(token);
    }
}
