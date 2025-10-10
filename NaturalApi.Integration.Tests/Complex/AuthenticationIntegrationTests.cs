using Microsoft.Extensions.DependencyInjection;
using NaturalApi;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Complex;

[TestClass]
public class AuthenticationIntegrationTests
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        _serviceProvider = ServiceConfiguration.ConfigureServices(
            authServerUrl: _wireMockServers.AuthBaseUrl,
            apiServerUrl: _wireMockServers.ApiBaseUrl,
            username: "testuser",
            password: "testpass",
            cacheExpiration: TimeSpan.FromMinutes(10));

        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public void FirstTimeAuthentication_ShouldSucceed()
    {
        // Act - Make API call (should trigger authentication)
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        
        var responseBody = result.RawBody;
        Assert.IsTrue(responseBody.Contains("Access granted"));
    }

    [TestMethod]
    public void CachedToken_ShouldBeUsed_OnSubsequentCalls()
    {
        // Act - Make first call (triggers authentication)
        var result1 = _api.For("/api/protected").Get();
        
        // Make second call (should use cached token)
        var result2 = _api.For("/api/protected").Get();

        // Assert both calls succeed
        Assert.AreEqual(200, result1.StatusCode);
        Assert.AreEqual(200, result2.StatusCode);
    }

    [TestMethod]
    public void MultipleUsers_ShouldHaveSeparateTokenCaches()
    {
        // Arrange - Create service provider for second user
        var serviceProvider2 = ServiceConfiguration.ConfigureServices(
            authServerUrl: _wireMockServers.AuthBaseUrl,
            apiServerUrl: _wireMockServers.ApiBaseUrl,
            username: "user2",
            password: "pass2",
            cacheExpiration: TimeSpan.FromMinutes(10));

        var api2 = serviceProvider2.GetRequiredService<IApi>();

        try
        {
            // Act - Make calls with both users
            var result1 = _api.For("/api/protected").Get();
            var result2 = api2.For("/api/protected").Get();

            // Assert both succeed with different responses
            Assert.AreEqual(200, result1.StatusCode);
            Assert.AreEqual(200, result2.StatusCode);
            
            var response1 = result1.RawBody;
            var response2 = result2.RawBody;
            
            Assert.IsTrue(response1.Contains("Access granted"));
            Assert.IsTrue(response2.Contains("Access granted for user2"));
        }
        finally
        {
            if (serviceProvider2 is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [TestMethod]
    public void InvalidCredentials_ShouldFail()
    {
        // Arrange - Create service provider with invalid credentials
        var invalidServiceProvider = ServiceConfiguration.ConfigureServices(
            authServerUrl: _wireMockServers.AuthBaseUrl,
            apiServerUrl: _wireMockServers.ApiBaseUrl,
            username: "invalid",
            password: "invalid",
            cacheExpiration: TimeSpan.FromMinutes(10));

        var invalidApi = invalidServiceProvider.GetRequiredService<IApi>();

        try
        {
            // Act - Make API call with invalid credentials
            var result = invalidApi.For("/api/protected").Get();

            // Assert - Should fail due to authentication failure
            Assert.AreNotEqual(200, result.StatusCode);
        }
        finally
        {
            if (invalidServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [TestMethod]
    public void TokenExpiration_ShouldTriggerNewAuthentication()
    {
        // Arrange - Create service provider with very short cache expiration
        var shortCacheServiceProvider = ServiceConfiguration.ConfigureServices(
            authServerUrl: _wireMockServers.AuthBaseUrl,
            apiServerUrl: _wireMockServers.ApiBaseUrl,
            username: "testuser",
            password: "testpass",
            cacheExpiration: TimeSpan.FromMilliseconds(100)); // Very short cache

        var shortCacheApi = shortCacheServiceProvider.GetRequiredService<IApi>();

        try
        {
            // Act - Make first call
            var result1 = shortCacheApi.For("/api/protected").Get();
            
            // Wait for cache to expire
            Thread.Sleep(200);
            
            // Make second call (should trigger new authentication)
            var result2 = shortCacheApi.For("/api/protected").Get();

            // Assert both calls succeed (new auth was triggered)
            Assert.AreEqual(200, result1.StatusCode);
            Assert.AreEqual(200, result2.StatusCode);
        }
        finally
        {
            if (shortCacheServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task AuthService_ShouldCacheTokensPerUser()
    {
        // Arrange - Get auth service directly
        var authService = _serviceProvider.GetRequiredService<IUsernamePasswordAuthService>();

        // Act - Authenticate first time
        var token1 = await authService.AuthenticateAsync("testuser", "testpass");
        
        // Get cached token
        var cachedToken = await authService.GetCachedTokenAsync("testuser");
        
        // Authenticate again (should use cache)
        var token2 = await authService.AuthenticateAsync("testuser", "testpass");

        // Assert
        Assert.IsNotNull(token1);
        Assert.IsNotNull(cachedToken);
        Assert.IsNotNull(token2);
        Assert.AreEqual(token1, cachedToken);
        Assert.AreEqual(token1, token2); // Should be same token from cache
    }

    [TestMethod]
    public void EndToEnd_AuthenticationFlow_ShouldWork()
    {
        // This test demonstrates the complete flow:
        // 1. User makes API call
        // 2. NaturalApi detects no auth token
        // 3. Auth provider calls auth service
        // 4. Auth service authenticates with WireMock
        // 5. Token is cached
        // 6. Token is added to request
        // 7. API call succeeds with bearer token

        // Act
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        
        var responseBody = result.RawBody;
        Assert.IsTrue(responseBody.Contains("Access granted"));
        Assert.IsTrue(responseBody.Contains("protected-resource-data"));
    }
}