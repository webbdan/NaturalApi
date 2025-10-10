using Microsoft.Extensions.DependencyInjection;
using NaturalApi;
using System.Net.Http;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Simple;

[TestClass]
public class SimpleAuthIntegrationTests
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        // Configure services - NaturalApi handles everything!
        var services = new ServiceCollection();
        
        // Configure HttpClient for auth service (auth provider needs its own client)
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });
        
        // Clean pattern - auth provider knows its own URLs, API gets base URL
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl,      // API base URL
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) }, 
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
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
    public async Task SimpleAuth_WithCredentials_ShouldWork()
    {
        // Act - Use the new AsUser(username, password) method
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task SimpleAuth_WithDifferentCredentials_ShouldWork()
    {
        // Act - Use different credentials (user2 is configured in WireMock)
        var result = _api.For("/api/protected").AsUser("user2", "pass2").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted for user2"));
    }

    [TestMethod]
    public async Task SimpleAuth_WithoutCredentials_ShouldFail()
    {
        // Act - No credentials provided
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Unauthorized"));
    }

    [TestMethod]
    public async Task SimpleAuth_WithInvalidCredentials_ShouldFail()
    {
        // Act - Invalid credentials
        var result = _api.For("/api/protected").AsUser("invaliduser", "wrongpass").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Unauthorized"));
    }

    [TestMethod]
    public async Task SimpleAuth_UsernameOnly_ShouldFail()
    {
        // Act - Only username, no password
        var result = _api.For("/api/protected").AsUser("testuser").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Unauthorized"));
    }
}
