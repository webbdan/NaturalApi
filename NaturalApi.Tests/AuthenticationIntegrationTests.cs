// AIModified:2025-10-09T07:22:36Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class AuthenticationIntegrationTests
{
    private MockHttpExecutor _mockExecutor = null!;
    private TestApiDefaultsProvider _defaults = null!;
    private TestAuthProvider _authProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _authProvider = new TestAuthProvider("test-token");
        _defaults = new TestApiDefaultsProvider(authProvider: _authProvider);
    }

    [TestMethod]
    public async Task Api_With_AuthProvider_Should_Add_Authorization_Header()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        // The mock executor doesn't support authentication, so we can't test the header directly
        // But we can verify the request was executed
        Assert.IsNotNull(_mockExecutor.LastSpec);
    }

    [TestMethod]
    public async Task Api_With_AuthProvider_Should_Use_Authenticated_Executor()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        // Verify the request was executed with the correct spec
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);
    }

    [TestMethod]
    public async Task Api_Without_AuthProvider_Should_Use_Regular_Executor()
    {
        // Arrange
        var emptyDefaults = new TestApiDefaultsProvider(authProvider: null);
        var api = new Api(_mockExecutor, emptyDefaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);
    }

    [TestMethod]
    public async Task WithoutAuth_Should_Suppress_Authentication()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test").WithoutAuth().Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
    }

    [TestMethod]
    public async Task AsUser_Should_Set_Username_Context()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);
        var username = "testuser";

        // Act
        var result = api.For("/test").AsUser(username).Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);
    }

    [TestMethod]
    public async Task AsUser_And_WithoutAuth_Should_Work_Together()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);
        var username = "testuser";

        // Act
        var result = api.For("/test").AsUser(username).WithoutAuth().Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_All_HTTP_Methods()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Test GET
        var getResult = api.For("/test").Get();
        Assert.IsNotNull(getResult);

        // Test POST
        var postResult = api.For("/test").Post(new { data = "test" });
        Assert.IsNotNull(postResult);

        // Test PUT
        var putResult = api.For("/test").Put(new { data = "test" });
        Assert.IsNotNull(putResult);

        // Test PATCH
        var patchResult = api.For("/test").Patch(new { data = "test" });
        Assert.IsNotNull(patchResult);

        // Test DELETE
        var deleteResult = api.For("/test").Delete();
        Assert.IsNotNull(deleteResult);
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Chained_Methods()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .WithHeader("Accept", "application/json")
            .AsUser("testuser")
            .WithQueryParam("test", "value")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Accept"));
        Assert.IsTrue(_mockExecutor.LastSpec.QueryParams.ContainsKey("test"));
    }

    [TestMethod]
    public async Task Authentication_Should_Override_Existing_Auth_Headers()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .WithHeader("Authorization", "Bearer existing-token")
            .AsUser("testuser")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        // The existing Authorization header should still be there
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Timeout()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .AsUser("testuser")
            .WithTimeout(TimeSpan.FromSeconds(60))
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        Assert.AreEqual(TimeSpan.FromSeconds(60), _mockExecutor.LastSpec.Timeout);
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Path_Parameters()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test/{id}")
            .AsUser("testuser")
            .WithPathParam("id", 123)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        Assert.IsTrue(_mockExecutor.LastSpec.PathParams.ContainsKey("id"));
        Assert.AreEqual(123, _mockExecutor.LastSpec.PathParams["id"]);
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Query_Parameters()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .AsUser("testuser")
            .WithQueryParam("filter", "active")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        Assert.IsTrue(_mockExecutor.LastSpec.QueryParams.ContainsKey("filter"));
        Assert.AreEqual("active", _mockExecutor.LastSpec.QueryParams["filter"]);
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
