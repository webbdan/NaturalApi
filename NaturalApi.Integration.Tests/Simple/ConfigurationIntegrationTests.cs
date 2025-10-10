using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NaturalApi;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Simple;

/// <summary>
/// Integration tests showing NaturalApi usage with configuration from appsettings.json
/// and various DI patterns including overrides.
/// </summary>
[TestClass]
public class ConfigurationIntegrationTests
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
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
    public async Task Configuration_FromAppSettings_ShouldWork()
    {
        // Arrange - Create configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Simple/appsettings.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl,
                ["ApiSettings:AuthEndpoint"] = "/auth/login"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Configure HttpClient for auth service
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use configuration to register NaturalApi
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Configuration_WithCodeOverride_ShouldWork()
    {
        // Arrange - Configuration with code override
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = "https://default-api.example.com", // This will be overridden
                ["ApiSettings:AuthBaseUrl"] = "https://default-auth.example.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Override with actual test URLs
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl, // Override the config value
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Configuration_UltraSimple_ShouldWork()
    {
        // Arrange - Ultra simple configuration
        var services = new ServiceCollection();
        
        // Just register NaturalApi with no configuration
        services.AddNaturalApi();

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Must use absolute URLs
        var result = _api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task Configuration_WithBaseUrlOnly_ShouldWork()
    {
        // Arrange - Configuration with base URL only
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use configuration value
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl(apiBaseUrl!));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint (no auth)
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task Configuration_WithAuthProviderOnly_ShouldWork()
    {
        // Arrange - Configuration with auth provider only
        var services = new ServiceCollection();
        
        // Register auth provider without base URL
        services.AddNaturalApi(new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login"));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Must use absolute URLs but with auth
        var result = _api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Configuration_WithNamedHttpClient_ShouldWork()
    {
        // Arrange - Configuration with named HttpClient
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:HttpClientName"] = "MyApiClient"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Configure named HttpClient
        var httpClientName = configuration["ApiSettings:HttpClientName"];
        services.AddHttpClient(httpClientName!, client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        // Register auth provider
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use named HttpClient with auth
        services.AddNaturalApi(httpClientName!, new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login"));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
