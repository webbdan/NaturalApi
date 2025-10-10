using NaturalApi;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Simple;

/// <summary>
/// Integration tests showing NaturalApi usage WITHOUT dependency injection.
/// This demonstrates the simplest possible usage patterns.
/// </summary>
[TestClass]
public class NoDiIntegrationTests
{
    private WireMockServers _wireMockServers = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task NoDi_UltraSimpleUsage_ShouldWork()
    {
        // Arrange - Ultra simple usage - no base URL needed, just use absolute URLs
        var api = new Api();

        // Act - Use absolute URL directly
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task NoDi_SimplestUsage_ShouldWork()
    {
        // Arrange - Simplest possible usage - just base URL
        var api = new Api(_wireMockServers.ApiBaseUrl);

        // Act - Use relative endpoint
        var result = api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task NoDi_AbsoluteUrl_ShouldWork()
    {
        // Arrange - Create API with absolute URL (no base URL needed)
        var api = new Api($"{_wireMockServers.ApiBaseUrl}/api/protected");

        // Act - Use absolute URL directly
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task NoDi_WithBaseUrl_ShouldWork()
    {
        // Arrange - Create API with base URL, then use relative endpoints
        var api = new Api(_wireMockServers.ApiBaseUrl);

        // Act - Use relative endpoint
        var result = api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task NoDi_WithBaseUrlAndAuth_ShouldWork()
    {
        // Arrange - Create API with base URL and auth provider
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) };
        var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        var api = new Api(defaults, new HttpClient { BaseAddress = new Uri(_wireMockServers.ApiBaseUrl) });

        // Act - Use relative endpoint with authentication
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task NoDi_WithAuthProvider_ShouldWork()
    {
        // Arrange - Create auth provider and API with auth
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) };
        var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        var httpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.ApiBaseUrl) };
        var api = new Api(defaults, httpClient);

        // Act - Use relative endpoint with authentication
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task NoDi_AbsoluteUrlWithAuth_ShouldWork()
    {
        // Arrange - Create auth provider and API with absolute URL
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) };
        var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        var httpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.ApiBaseUrl) };
        var api = new Api(defaults, httpClient);

        // Act - Use absolute URL with authentication
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
