using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NaturalApi;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Simple;

/// <summary>
/// Configuration options for NaturalApi settings.
/// </summary>
public class NaturalApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthBaseUrl { get; set; } = string.Empty;
    public string AuthEndpoint { get; set; } = "/auth/login";
    public string HttpClientName { get; set; } = "NaturalApiClient";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Advanced integration tests showing NaturalApi usage with strongly-typed configuration
/// and various DI patterns including environment-specific overrides.
/// </summary>
[TestClass]
public class AdvancedConfigurationTests
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
    public async Task Configuration_StronglyTyped_ShouldWork()
    {
        // Arrange - Create configuration with strongly-typed settings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Simple/appsettings.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NaturalApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["NaturalApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl,
                ["NaturalApiSettings:AuthEndpoint"] = "/auth/login"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Bind strongly-typed configuration
        var apiSettings = new NaturalApiSettings();
        configuration.GetSection("NaturalApiSettings").Bind(apiSettings);
        services.AddSingleton(apiSettings);

        // Configure HttpClient for auth service
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(apiSettings.AuthBaseUrl);
        });

        // Use strongly-typed configuration
        services.AddNaturalApiWithBaseUrl(
            apiSettings.BaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(apiSettings.AuthBaseUrl) },
                apiSettings.AuthEndpoint));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Configuration_EnvironmentSpecific_ShouldWork()
    {
        // Arrange - Environment-specific configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Default settings
                ["ApiSettings:BaseUrl"] = "https://api.example.com",
                ["ApiSettings:AuthBaseUrl"] = "https://auth.example.com"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Test environment overrides
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use environment-specific values
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        var authBaseUrl = configuration["ApiSettings:AuthBaseUrl"];
        
        services.AddNaturalApiWithBaseUrl(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(authBaseUrl!) },
                "/auth/login"));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Configuration_WithOptionsPattern_ShouldWork()
    {
        // Arrange - Using Options pattern
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NaturalApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["NaturalApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl,
                ["NaturalApiSettings:AuthEndpoint"] = "/auth/login"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Configure options
        services.Configure<NaturalApiSettings>(configuration.GetSection("NaturalApiSettings"));
        
        // Configure HttpClients
        services.AddHttpClient("AuthClient", (provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<NaturalApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.AuthBaseUrl);
        });

        // Register NaturalApi using options
        services.AddScoped<IApi>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<NaturalApiSettings>>().Value;
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("AuthClient");
            var authProvider = new SimpleCustomAuthProvider(httpClient, settings.AuthEndpoint);
            var defaults = new DefaultApiDefaults(authProvider: authProvider);
            return new Api(defaults, new HttpClient { BaseAddress = new Uri(settings.BaseUrl) });
        });

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Configuration_WithMultipleEnvironments_ShouldWork()
    {
        // Arrange - Multiple environment configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Development settings
                ["Development:ApiSettings:BaseUrl"] = "https://dev-api.example.com",
                ["Development:ApiSettings:AuthBaseUrl"] = "https://dev-auth.example.com"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Test settings (override development)
                ["Test:ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["Test:ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use test environment settings
        var apiBaseUrl = configuration["Test:ApiSettings:BaseUrl"];
        var authBaseUrl = configuration["Test:ApiSettings:AuthBaseUrl"];
        
        services.AddNaturalApiWithBaseUrl(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(authBaseUrl!) },
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
