using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using NaturalApi.Integration.Tests.Playwright.Executors;
using NaturalApi.Integration.Tests.Playwright.Common;

namespace NaturalApi.Integration.Tests.Playwright.Tests;

/// <summary>
/// Tests proving that generic executor registration works with Playwright.
/// </summary>
[TestClass]
public class PlaywrightRegistrationTests
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
    public void AddNaturalApi_WithPlaywrightExecutor_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor("https://api.example.com"));

        // Act
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();
        var executor = _serviceProvider.GetRequiredService<IHttpExecutor>();

        // Assert
        Assert.IsNotNull(api);
        Assert.IsNotNull(executor);
        Assert.IsInstanceOfType(executor, typeof(PlaywrightHttpExecutor));
    }

    [TestMethod]
    public void AddNaturalApi_WithPlaywrightExecutorFactory_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor("https://api.example.com"));

        // Act
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();
        var executor = _serviceProvider.GetRequiredService<IHttpExecutor>();

        // Assert
        Assert.IsNotNull(api);
        Assert.IsNotNull(executor);
        Assert.IsInstanceOfType(executor, typeof(PlaywrightHttpExecutor));
    }

    [TestMethod]
    public void AddNaturalApi_WithPlaywrightExecutorAndOptions_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor, PlaywrightOptions>(options =>
        {
            options.BaseUrl = "https://api.example.com";
            options.Timeout = TimeSpan.FromSeconds(30);
        });

        // Act
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();
        var executor = _serviceProvider.GetRequiredService<IHttpExecutor>();
        var options = _serviceProvider.GetRequiredService<PlaywrightOptions>();

        // Assert
        Assert.IsNotNull(api);
        Assert.IsNotNull(executor);
        Assert.IsInstanceOfType(executor, typeof(PlaywrightHttpExecutor));
        Assert.AreEqual("https://api.example.com", options.BaseUrl);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.Timeout);
    }

    [TestMethod]
    public void AddNaturalApi_WithPlaywrightExecutor_SupportsDefaultsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor("https://api.example.com"));
        services.AddSingleton<IApiDefaultsProvider, TestApiDefaults>();

        // Act
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();
        var defaults = _serviceProvider.GetRequiredService<IApiDefaultsProvider>();

        // Assert
        Assert.IsNotNull(api);
        Assert.IsNotNull(defaults);
        Assert.IsInstanceOfType(defaults, typeof(TestApiDefaults));
    }

    [TestMethod]
    public void AddNaturalApi_WithPlaywrightExecutor_SupportsAuthProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor("https://api.example.com"));
        services.AddSingleton<IApiAuthProvider, TestAuthProvider>();
        services.AddSingleton<IApiDefaultsProvider, TestApiDefaults>();

        // Act
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();
        var authProvider = _serviceProvider.GetRequiredService<IApiAuthProvider>();
        var defaults = _serviceProvider.GetRequiredService<IApiDefaultsProvider>();

        // Assert
        Assert.IsNotNull(api);
        Assert.IsNotNull(authProvider);
        Assert.IsNotNull(defaults);
        Assert.IsInstanceOfType(authProvider, typeof(TestAuthProvider));
    }

    [TestMethod]
    public void AddNaturalApi_WithPlaywrightExecutor_CanMakeRequests()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor(_wireMockServers!.BaseUrl));

        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        // Setup mock response
        _wireMockServers!.SetupGet("/test", 200, "Hello World");

        // Act & Assert
        var result = api.For("/test").Get();
        Assert.AreEqual("Hello World", result.RawBody);
    }
}

/// <summary>
/// Test options for Playwright executor.
/// </summary>
public class PlaywrightOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Test API defaults provider.
/// </summary>
public class TestApiDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri("https://api.example.com");
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider => null;
}
