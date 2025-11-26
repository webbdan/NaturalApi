using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NaturalApi;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Simple;

/// <summary>
/// Comprehensive tests showing ALL NaturalApi DI patterns and configuration options.
/// This demonstrates the complete flexibility of NaturalApi registration.
/// </summary>
[TestClass]
public class AllDiPatternsTests
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
    public async Task DiPattern_UltraSimple_ShouldWork()
    {
        // Pattern 1: Ultra Simple - No configuration, must use absolute URLs
        var services = new ServiceCollection();
        services.AddNaturalApi();

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Must use absolute URLs
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task DiPattern_WithBaseUrl_ShouldWork()
    {
        // Pattern 2: With Base URL - Can use relative URLs
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl(_wireMockServers.ApiBaseUrl));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs
        var result = api.For("/api/protected").Get();

        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task DiPattern_WithAuthProvider_ShouldWork()
    {
        // Pattern 3: With Auth Provider - Auth provider knows its own URLs
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithAuth(new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Must use absolute URLs but with auth
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task DiPattern_WithBaseUrlAndAuth_ShouldWork()
    {
        // Pattern 4: With Both - Best of both worlds
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task DiPattern_WithNamedHttpClient_ShouldWork()
    {
        // Pattern 5: With Named HttpClient
        var services = new ServiceCollection();
        
        // Configure named HttpClient
        services.AddHttpClient("MyApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        services.AddHttpClient("MyAuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use named HttpClient with auth
        services.AddNaturalApi(NaturalApiConfiguration.WithHttpClientAndAuth("MyApiClient", new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task DiPattern_WithConfiguration_ShouldWork()
    {
        // Pattern 6: With Configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use configuration values
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task DiPattern_WithFactory_ShouldWork()
    {
        // Pattern 7: With Factory - Maximum flexibility
        var services = new ServiceCollection();
        
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use factory for maximum control
        services.AddNaturalApi(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            var authHttpClient = httpClientFactory.CreateClient("AuthClient");
            var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
            var defaults = new DefaultApiDefaults(authProvider: authProvider);
            return new Api(defaults, httpClient);
        });

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task DiPattern_WithCustomApi_ShouldWork()
    {
        // Pattern 8: With Custom API - Advanced scenarios
        var services = new ServiceCollection();
        
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Register auth provider
        services.AddSingleton<IApiAuthProvider>(new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login"));

        // Register defaults
        services.AddSingleton<IApiDefaultsProvider>(provider =>
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });

        // Use custom API factory
        services.AddNaturalApi<SimpleCustomAuthProvider, CustomApi>(
            "ApiClient",
            provider => new SimpleCustomAuthProvider(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("AuthClient"),
                "/auth/login"),
            provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var executor = new HttpClientExecutor(httpClient, null);
                var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
                return new CustomApi(executor, defaults, httpClient);
            });

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
