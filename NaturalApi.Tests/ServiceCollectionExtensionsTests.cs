using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using NaturalApi.Reporter;

namespace NaturalApi.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
    // ──────────────────────────────────────────────────────────────
    //  Builder pattern tests (new primary API)
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Builder_Default_Should_Register_IApi()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(api => { });

        var sp = services.BuildServiceProvider();
        var api = sp.GetService<IApi>();
        Assert.IsNotNull(api);
        Assert.IsInstanceOfType(api, typeof(Api));
    }

    [TestMethod]
    public void Builder_Default_Should_Register_DefaultsProvider()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(api => { });

        var sp = services.BuildServiceProvider();
        var defaults = sp.GetService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults);
        Assert.IsInstanceOfType(defaults, typeof(DefaultApiDefaults));
    }

    [TestMethod]
    public void Builder_WithBaseUrl_Should_Register_IApi()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(api => api.BaseUrl = "https://api.example.com");

        var sp = services.BuildServiceProvider();
        var api = sp.GetService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void Builder_WithAuth_Instance_Should_Register_AuthProvider()
    {
        var services = new ServiceCollection();
        var authProvider = new TestAuthProvider("test-token");

        services.AddNaturalApi(api => api.WithAuth(authProvider));

        var sp = services.BuildServiceProvider();
        var registeredAuth = sp.GetRequiredService<IApiAuthProvider>();
        Assert.AreEqual(authProvider, registeredAuth);
    }

    [TestMethod]
    public void Builder_WithAuth_Instance_Should_Register_DefaultsWithAuth()
    {
        var services = new ServiceCollection();
        var authProvider = new TestAuthProvider("test-token");

        services.AddNaturalApi(api => api.WithAuth(authProvider));

        var sp = services.BuildServiceProvider();
        var defaults = sp.GetRequiredService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults.AuthProvider);
    }

    [TestMethod]
    public void Builder_WithAuth_Factory_Should_Resolve_AuthProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected-token");

        services.AddNaturalApi(api =>
        {
            api.WithAuth(sp => new TestAuthProvider(sp.GetRequiredService<string>()));
        });

        var sp = services.BuildServiceProvider();
        var auth = sp.GetRequiredService<IApiAuthProvider>();
        Assert.IsNotNull(auth);
        var token = auth.GetAuthTokenAsync().Result;
        Assert.AreEqual("injected-token", token);
    }

    [TestMethod]
    public void Builder_WithBaseUrlAndAuth_Should_Register_Both()
    {
        var services = new ServiceCollection();
        var authProvider = new TestAuthProvider("test-token");

        services.AddNaturalApi(api =>
        {
            api.WithBaseUrl("https://api.example.com");
            api.WithAuth(authProvider);
        });

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        var defaults = sp.GetRequiredService<IApiDefaultsProvider>();
        Assert.IsNotNull(api);
        Assert.IsNotNull(defaults.AuthProvider);
    }

    [TestMethod]
    public void Builder_WithNamedHttpClient_Should_Register_IApi()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("MyClient");

        services.AddNaturalApi(api => api.WithHttpClient("MyClient"));

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void Builder_WithReporter_Should_Resolve_Reporter()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(api => api.WithReporter("compact"));

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
        // The reporter is applied internally — we verify no exception is thrown
    }

    [TestMethod]
    public void Builder_WithFactory_Should_Use_Factory()
    {
        var services = new ServiceCollection();
        var customApi = new Api();

        services.AddNaturalApi(api => api.WithFactory(_ => customApi));

        var sp = services.BuildServiceProvider();
        var resolved = sp.GetRequiredService<IApi>();
        Assert.AreEqual(customApi, resolved);
    }

    [TestMethod]
    public void Builder_WithExecutorType_Should_Register_Executor()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(api => api.WithExecutor<MockHttpExecutor>());

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void Builder_WithExecutorFactory_Should_Register_Executor()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(api =>
        {
            api.WithExecutor(_ => new MockHttpExecutor());
        });

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void Builder_Should_Be_Chainable()
    {
        var services = new ServiceCollection();
        var result = services.AddNaturalApi(api =>
        {
            api.WithBaseUrl("https://api.example.com")
               .WithAuth(new TestAuthProvider("tok"))
               .WithReporter("compact");
        });

        Assert.AreEqual(services, result);
    }

    // ──────────────────────────────────────────────────────────────
    //  Backward-compatible overload tests (legacy API)
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddNaturalApi_Parameterless_Should_Register_IApi()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi();

        var sp = services.BuildServiceProvider();
        var api = sp.GetService<IApi>();
        Assert.IsNotNull(api);
        Assert.IsInstanceOfType(api, typeof(Api));
    }

    [TestMethod]
    public void AddNaturalApi_Parameterless_Should_Register_Default_Defaults_Provider()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi();

        var sp = services.BuildServiceProvider();
        var defaults = sp.GetService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults);
        Assert.IsInstanceOfType(defaults, typeof(DefaultApiDefaults));
    }

    [TestMethod]
    public void AddNaturalApi_With_Custom_Defaults_Should_Register_Custom_Provider()
    {
        var services = new ServiceCollection();
        var customDefaults = new TestApiDefaultsProvider();

        services.AddSingleton<IApiDefaultsProvider>(customDefaults);
        services.AddNaturalApi();

        var sp = services.BuildServiceProvider();
        var defaults = sp.GetRequiredService<IApiDefaultsProvider>();
        Assert.AreEqual(customDefaults, defaults);
    }

    [TestMethod]
    public void AddNaturalApiWithAuth_Should_Register_Auth_Provider()
    {
        var services = new ServiceCollection();
        var authProvider = new TestAuthProvider("test-token");

        services.AddNaturalApi(authProvider);

        var sp = services.BuildServiceProvider();
        var registeredAuthProvider = sp.GetRequiredService<IApiAuthProvider>();
        Assert.AreEqual(authProvider, registeredAuthProvider);
    }

    [TestMethod]
    public void AddNaturalApiWithAuth_Should_Register_Defaults_Provider_With_Auth()
    {
        var services = new ServiceCollection();
        var authProvider = new TestAuthProvider("test-token");

        services.AddNaturalApi(authProvider);

        var sp = services.BuildServiceProvider();
        var defaults = sp.GetRequiredService<IApiDefaultsProvider>();
        Assert.IsNotNull(defaults);
        Assert.IsNotNull(defaults.AuthProvider);
        Assert.AreEqual(authProvider, defaults.AuthProvider);
    }

    [TestMethod]
    public void AddNaturalApiWithAuth_Both_Providers_Should_Register_Both()
    {
        var services = new ServiceCollection();
        var defaultsProvider = new TestApiDefaultsProvider();
        var authProvider = new TestAuthProvider("test-token");

        services.AddSingleton<IApiDefaultsProvider>(defaultsProvider);
        services.AddSingleton<IApiAuthProvider>(authProvider);
        services.AddNaturalApi();

        var sp = services.BuildServiceProvider();
        var registeredDefaults = sp.GetRequiredService<IApiDefaultsProvider>();
        var registeredAuth = sp.GetRequiredService<IApiAuthProvider>();

        Assert.AreEqual(defaultsProvider, registeredDefaults);
        Assert.AreEqual(authProvider, registeredAuth);
    }

    [TestMethod]
    public void AddNaturalApi_With_Factory_Should_Use_Factory()
    {
        var services = new ServiceCollection();
        var customApi = new Api();

        services.AddNaturalApi(_ => customApi);

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.AreEqual(customApi, api);
    }

    [TestMethod]
    public void AddNaturalApi_With_Config_Should_Register_IApi()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl("https://api.example.com"));

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void AddNaturalApi_Should_Be_Chainable()
    {
        var services = new ServiceCollection();
        var result = services.AddNaturalApi();
        Assert.AreEqual(services, result);
    }

    [TestMethod]
    public void AddNaturalApi_Should_Work_With_Named_HttpClient()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("TestClient");

        services.AddNaturalApi("TestClient");

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void AddNaturalApi_Should_Handle_Multiple_Registrations()
    {
        var services = new ServiceCollection();
        services.AddNaturalApi();
        services.AddNaturalApi(); // Should not throw

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void AddNaturalApi_WithNamedClient_AndAuth_Should_Register_Both()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("MyApi");
        var authProvider = new TestAuthProvider("tok");

        services.AddNaturalApi("MyApi", authProvider);

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        var auth = sp.GetRequiredService<IApiAuthProvider>();
        Assert.IsNotNull(api);
        Assert.AreEqual(authProvider, auth);
    }

    [TestMethod]
    public void AddNaturalApiWithBaseUrl_Should_Register_Auth_And_Api()
    {
        var services = new ServiceCollection();
        var authProvider = new TestAuthProvider("tok");

        services.AddNaturalApiWithBaseUrl("https://api.example.com", authProvider);

        var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IApi>();
        var auth = sp.GetRequiredService<IApiAuthProvider>();
        Assert.IsNotNull(api);
        Assert.AreEqual(authProvider, auth);
    }

    // ──────────────────────────────────────────────────────────────
    //  Reporting tests
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddNaturalApiReporting_Should_Register_Factory()
    {
        var services = new ServiceCollection();
        services.AddNaturalApiReporting();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<IReporterFactory>();
        Assert.IsNotNull(factory);
    }

    [TestMethod]
    public void AddNaturalApiReporting_Should_Resolve_Default_Reporter()
    {
        var services = new ServiceCollection();
        services.AddNaturalApiReporting();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IReporterFactory>();
        var reporter = factory.Get("default");
        Assert.IsNotNull(reporter);
        Assert.IsInstanceOfType(reporter, typeof(DefaultReporter));
    }

    [TestMethod]
    public void AddNaturalApiReporting_Should_Resolve_Compact_Reporter()
    {
        var services = new ServiceCollection();
        services.AddNaturalApiReporting();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IReporterFactory>();
        var reporter = factory.Get("compact");
        Assert.IsNotNull(reporter);
        Assert.IsInstanceOfType(reporter, typeof(CompactReporter));
    }

    [TestMethod]
    public void AddNaturalApiReporting_Should_Resolve_Null_Reporter()
    {
        var services = new ServiceCollection();
        services.AddNaturalApiReporting();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IReporterFactory>();
        var reporter = factory.Get("null");
        Assert.IsNotNull(reporter);
        Assert.IsInstanceOfType(reporter, typeof(NullReporter));
    }

    // ──────────────────────────────────────────────────────────────
    //  Test helpers
    // ──────────────────────────────────────────────────────────────

    private class TestApiDefaultsProvider : IApiDefaultsProvider
    {
        public Uri? BaseUri => new Uri("https://api.test.com/");
        public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public IApiAuthProvider? AuthProvider => null;
    }

    public class TestAuthProvider : IApiAuthProvider
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
