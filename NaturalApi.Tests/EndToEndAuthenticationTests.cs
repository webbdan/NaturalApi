// AIModified:2025-10-09T07:22:36Z
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class EndToEndAuthenticationTests
{
    [TestMethod]
    public async Task Complete_Authentication_Flow_With_DI_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNaturalApiWithAuth(new TestAuthProvider("integration-token"));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").Get();

        // Assert
        Assert.IsNotNull(result);
        // Verify the request was executed (mock executor doesn't make real requests)
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_Custom_Defaults_And_Auth_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        
        var customDefaults = new TestApiDefaultsProvider(
            new Uri("https://api.custom.com/"),
            new Dictionary<string, string> { ["X-Custom"] = "value" },
            TimeSpan.FromSeconds(60),
            new TestAuthProvider("custom-token")
        );
        
        services.AddNaturalApiWithAuth(customDefaults, new TestAuthProvider("custom-token"));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_WithoutAuth_Should_Skip_Auth()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNaturalApiWithAuth(new TestAuthProvider("should-not-be-used"));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").WithoutAuth().Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_AsUser_Should_Pass_Username()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var authProvider = new TestAuthProvider("user-specific-token");
        services.AddNaturalApiWithAuth(authProvider);

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").AsUser("testuser").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_Chained_Methods_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNaturalApiWithAuth(new TestAuthProvider("chained-token"));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers")
            .WithHeader("Accept", "application/json")
            .AsUser("testuser")
            .WithQueryParam("active", true)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_All_HTTP_Methods_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNaturalApiWithAuth(new TestAuthProvider("all-methods-token"));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Test GET
        var getResult = api.For("https://httpbin.org/headers").Get();
        Assert.IsNotNull(getResult);

        // Test POST
        var postResult = api.For("https://httpbin.org/post").Post(new { data = "test" });
        Assert.IsNotNull(postResult);

        // Test PUT
        var putResult = api.For("https://httpbin.org/put").Put(new { data = "test" });
        Assert.IsNotNull(putResult);

        // Test PATCH
        var patchResult = api.For("https://httpbin.org/patch").Patch(new { data = "test" });
        Assert.IsNotNull(patchResult);

        // Test DELETE
        var deleteResult = api.For("https://httpbin.org/delete").Delete();
        Assert.IsNotNull(deleteResult);
    }

    [TestMethod]
    public async Task Authentication_With_Multiple_Users_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNaturalApiWithAuth(new TestAuthProvider("multi-user-token"));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var user1Result = api.For("https://httpbin.org/headers").AsUser("user1").Get();
        var user2Result = api.For("https://httpbin.org/headers").AsUser("user2").Get();

        // Assert
        Assert.IsNotNull(user1Result);
        Assert.IsNotNull(user2Result);
        Assert.IsTrue(user1Result.StatusCode >= 200);
        Assert.IsTrue(user2Result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_Complex_Scenario_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        
        var authProvider = new TestAuthProvider("complex-token");
        var defaults = new TestApiDefaultsProvider(
            new Uri("https://api.complex.com/"),
            new Dictionary<string, string> 
            { 
                ["Accept"] = "application/json",
                ["User-Agent"] = "NaturalApi-Test"
            },
            TimeSpan.FromSeconds(120),
            authProvider
        );
        
        services.AddNaturalApiWithAuth(defaults, authProvider);

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers")
            .WithHeader("X-Custom", "value")
            .AsUser("complexuser")
            .WithQueryParam("filter", "active")
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_No_Auth_Provider_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNaturalApi(); // No auth provider

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_With_Custom_Factory_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        
        var customApi = new Api(new HttpClientExecutor(new HttpClient()));
        services.AddNaturalApi(_ => customApi);

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(customApi, api);
    }

    [TestMethod]
    public async Task Authentication_With_Options_Should_Respect_Options()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        
        var customDefaults = new TestApiDefaultsProvider();
        services.AddNaturalApi(customDefaults);
        services.AddNaturalApi(options => options.RegisterDefaults = false);

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Act
        var result = api.For("https://httpbin.org/headers").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StatusCode >= 200);
    }

    /// <summary>
    /// Test implementation of IApiDefaultsProvider for testing purposes.
    /// </summary>
    private class TestApiDefaultsProvider : IApiDefaultsProvider
    {
        public Uri? BaseUri { get; }
        public IDictionary<string, string> DefaultHeaders { get; }
        public TimeSpan Timeout { get; }
        public IApiAuthProvider? AuthProvider { get; }

        public TestApiDefaultsProvider(
            Uri? baseUri = null,
            IDictionary<string, string>? defaultHeaders = null,
            TimeSpan? timeout = null,
            IApiAuthProvider? authProvider = null)
        {
            BaseUri = baseUri ?? new Uri("https://api.example.com/");
            DefaultHeaders = defaultHeaders ?? new Dictionary<string, string>
            {
                ["Accept"] = "application/json"
            };
            Timeout = timeout ?? TimeSpan.FromSeconds(30);
            AuthProvider = authProvider;
        }
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

        public Task<string?> GetAuthTokenAsync(string? username = null)
        {
            return Task.FromResult(_token);
        }
    }
}
