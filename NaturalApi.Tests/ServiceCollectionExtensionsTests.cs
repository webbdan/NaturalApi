using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddNaturalApi_Should_Register_IApi()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();

        // Act
        services.AddNaturalApi();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetService<IApi>();
        Assert.IsNotNull(api);
        Assert.IsInstanceOfType(api, typeof(Api));
    }

    [TestMethod]
    public void AddNaturalApi_Should_Register_Default_Defaults_Provider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();

        // Act
        services.AddNaturalApi();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var defaults = serviceProvider.GetService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults);
        Assert.IsInstanceOfType(defaults, typeof(DefaultApiDefaults));
    }

    [TestMethod]
    public void AddNaturalApi_With_Custom_Defaults_Should_Register_Custom_Provider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var customDefaults = new TestApiDefaultsProvider();

        // Act
        services.AddSingleton<IApiDefaultsProvider>(customDefaults);
        services.AddNaturalApi();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var defaults = serviceProvider.GetRequiredService<IApiDefaultsProvider>();
        Assert.AreEqual(customDefaults, defaults);
    }

    [TestMethod]
    public void AddNaturalApiWithAuth_Should_Register_Auth_Provider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var authProvider = new TestAuthProvider("test-token");

        // Act
        services.AddNaturalApi(authProvider);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var registeredAuthProvider = serviceProvider.GetRequiredService<IApiAuthProvider>();
        Assert.AreEqual(authProvider, registeredAuthProvider);
    }

    [TestMethod]
    public void AddNaturalApiWithAuth_Should_Register_Defaults_Provider_With_Auth()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var authProvider = new TestAuthProvider("test-token");

        // Act
        services.AddNaturalApi(authProvider);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var defaults = serviceProvider.GetRequiredService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults);
        Assert.IsNotNull(defaults.AuthProvider);
        Assert.AreEqual(authProvider, defaults.AuthProvider);
    }

    [TestMethod]
    public void AddNaturalApiWithAuth_Both_Providers_Should_Register_Both()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var defaultsProvider = new TestApiDefaultsProvider();
        var authProvider = new TestAuthProvider("test-token");

        // Act
        services.AddSingleton<IApiDefaultsProvider>(defaultsProvider);
        services.AddSingleton<IApiAuthProvider>(authProvider);
        services.AddNaturalApi();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var registeredDefaults = serviceProvider.GetRequiredService<IApiDefaultsProvider>();
        var registeredAuth = serviceProvider.GetRequiredService<IApiAuthProvider>();
        
        Assert.AreEqual(defaultsProvider, registeredDefaults);
        Assert.AreEqual(authProvider, registeredAuth);
    }

    [TestMethod]
    public void AddNaturalApi_With_Factory_Should_Use_Factory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var customApi = new Api(new HttpClientExecutor(new HttpClient()));

        // Act
        services.AddNaturalApi(_ => customApi);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();
        Assert.AreEqual(customApi, api);
    }

    [TestMethod]
    public void AddNaturalApi_With_Options_Should_Respect_Options()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();

        // Act
        services.AddNaturalApi();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var defaults = serviceProvider.GetService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults);
    }

    [TestMethod]
    public void AddNaturalApi_Should_Be_Chainable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();

        // Act
        var result = services.AddNaturalApi();

        // Assert
        Assert.AreEqual(services, result);
    }

    [TestMethod]
    public void AddNaturalApi_Should_Work_With_Existing_HttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("TestClient");

        // Act
        services.AddNaturalApi();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void AddNaturalApi_Should_Handle_Multiple_Registrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();

        // Act
        services.AddNaturalApi();
        services.AddNaturalApi(); // Should not throw

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    /// <summary>
    /// Test implementation of IApiDefaultsProvider for testing purposes.
    /// </summary>
    private class TestApiDefaultsProvider : IApiDefaultsProvider
    {
        public Uri? BaseUri => new Uri("https://api.test.com/");
        public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public IApiAuthProvider? AuthProvider => null;
    }

    /// <summary>
    /// Test implementation of IApiAuthProvider for testing purposes.
    /// </summary>
    private class TestAuthProvider : IApiAuthProvider
    {
        private readonly string? _token;

        public TestAuthProvider(string? token)
        {
            _token = token;
        }

        public Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
        {
            return Task.FromResult(_token);
        }
    }
}
