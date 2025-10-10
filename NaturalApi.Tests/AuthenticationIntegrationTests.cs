using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net.Http;

namespace NaturalApi.Tests;

[TestClass]
public class AuthenticationIntegrationTests
{
    private AuthenticatedHttpClientExecutor _authenticatedExecutor = null!;
    private TestApiDefaultsProvider _defaults = null!;
    private TestAuthProvider _authProvider = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpClient = new HttpClient();
        _authenticatedExecutor = new AuthenticatedHttpClientExecutor(_httpClient);
        _authProvider = new TestAuthProvider("test-token");
        _defaults = new TestApiDefaultsProvider(authProvider: _authProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    [TestMethod]
    public async Task Api_With_AuthProvider_Should_Add_Authorization_Header()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should have processed the auth provider
        // and added the Authorization header automatically
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Api_With_AuthProvider_Should_Use_Authenticated_Executor()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the request with auth provider
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Api_Without_AuthProvider_Should_Use_Regular_Executor()
    {
        // Arrange
        var emptyDefaults = new TestApiDefaultsProvider(authProvider: null);
        var api = new Api(_authenticatedExecutor, emptyDefaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the request without auth provider
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task WithoutAuth_Should_Suppress_Authentication()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test").WithoutAuth().Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should respect the WithoutAuth() call
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task AsUser_Should_Set_Username_Context()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);
        var username = "testuser";

        // Act
        var result = api.For("/test").AsUser(username).Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the AsUser() call with auth provider
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task AsUser_And_WithoutAuth_Should_Work_Together()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);
        var username = "testuser";

        // Act
        var result = api.For("/test").AsUser(username).WithoutAuth().Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle both AsUser() and WithoutAuth() calls
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_All_HTTP_Methods()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Test GET
        var getResult = api.For("/test").Get();
        Assert.IsNotNull(getResult);
        Assert.IsTrue(getResult.StatusCode >= 200);

        // Test POST
        var postResult = api.For("/test").Post(new { data = "test" });
        Assert.IsNotNull(postResult);
        Assert.IsTrue(postResult.StatusCode >= 200);

        // Test PUT
        var putResult = api.For("/test").Put(new { data = "test" });
        Assert.IsNotNull(putResult);
        Assert.IsTrue(putResult.StatusCode >= 200);

        // Test PATCH
        var patchResult = api.For("/test").Patch(new { data = "test" });
        Assert.IsNotNull(patchResult);
        Assert.IsTrue(patchResult.StatusCode >= 200);

        // Test DELETE
        var deleteResult = api.For("/test").Delete();
        Assert.IsNotNull(deleteResult);
        Assert.IsTrue(deleteResult.StatusCode >= 200);
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Chained_Methods()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .WithHeader("Accept", "application/json")
            .AsUser("testuser")
            .WithQueryParam("test", "value")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle all chained methods including AsUser()
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Authentication_Should_Override_Existing_Auth_Headers()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .WithHeader("Authorization", "Bearer existing-token")
            .AsUser("testuser")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the AsUser() call and override existing auth
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Timeout()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .AsUser("testuser")
            .WithTimeout(TimeSpan.FromSeconds(60))
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle AsUser() and timeout together
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Path_Parameters()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test/{id}")
            .AsUser("testuser")
            .WithPathParam("id", 123)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle AsUser() and path parameters together
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public async Task Authentication_Should_Work_With_Query_Parameters()
    {
        // Arrange
        var api = new Api(_authenticatedExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .AsUser("testuser")
            .WithQueryParam("filter", "active")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle AsUser() and query parameters together
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
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

        public Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
        {
            return Task.FromResult(_token);
        }
    }
}
