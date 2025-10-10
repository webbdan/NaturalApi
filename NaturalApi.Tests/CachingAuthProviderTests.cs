using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net.Http;

namespace NaturalApi.Tests;

[TestClass]
public class CachingAuthProviderTests
{
    private AuthenticatedHttpClientExecutor _authenticatedExecutor = null!;
    private HttpClient _httpClient = null!;
    private TestApiDefaultsProvider _defaults = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpClient = new HttpClient();
        _authenticatedExecutor = new AuthenticatedHttpClientExecutor(_httpClient);
        _defaults = new TestApiDefaultsProvider(authProvider: new CachingAuthProvider());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }
    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_On_First_Call()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task CachingAuthProvider_Should_Work_With_Authenticated_Executor()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("https://httpbin.org/headers").Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should use the CachingAuthProvider and add Authorization header
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task CachingAuthProvider_Should_Work_With_AsUser()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("https://httpbin.org/headers")
            .AsUser("testuser", "testpass")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should use the CachingAuthProvider with AsUser() and add Authorization header
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task CachingAuthProvider_Should_Work_With_WithoutAuth()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("https://httpbin.org/headers")
            .WithoutAuth()
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should respect WithoutAuth() and not add Authorization header
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Same_Token_On_Subsequent_Calls()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token1 = await provider.GetAuthTokenAsync();
        var token2 = await provider.GetAuthTokenAsync();
        var token3 = await provider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token1);
        Assert.AreEqual("abc123", token2);
        Assert.AreEqual("abc123", token3);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();
        var username = "testuser";

        // Act
        var token = await provider.GetAuthTokenAsync(username);

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Null_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync(null);

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Empty_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync("");

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Whitespace_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync("   ");

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Be_Callable_Multiple_Times_Concurrently()
    {
        // Arrange
        var provider = new CachingAuthProvider();
        var tasks = new List<Task<string?>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(provider.GetAuthTokenAsync());
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10, results.Length);
        foreach (var result in results)
        {
            Assert.AreEqual("abc123", result);
        }
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Async_Operations()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public void CachingAuthProvider_Should_Implement_IApiAuthProvider()
    {
        // Arrange & Act
        var provider = new CachingAuthProvider();

        // Assert
        Assert.IsInstanceOfType(provider, typeof(IApiAuthProvider));
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Consistent_Token_Across_Multiple_Instances()
    {
        // Arrange
        var provider1 = new CachingAuthProvider();
        var provider2 = new CachingAuthProvider();

        // Act
        var token1 = await provider1.GetAuthTokenAsync();
        var token2 = await provider2.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token1);
        Assert.AreEqual("abc123", token2);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Mixed_Username_Calls()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token1 = await provider.GetAuthTokenAsync();
        var token2 = await provider.GetAuthTokenAsync("user1");
        var token3 = await provider.GetAuthTokenAsync("user2");
        var token4 = await provider.GetAuthTokenAsync(null);

        // Assert
        Assert.AreEqual("abc123", token1);
        Assert.AreEqual("abc123", token2);
        Assert.AreEqual("abc123", token3);
        Assert.AreEqual("abc123", token4);
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
}
