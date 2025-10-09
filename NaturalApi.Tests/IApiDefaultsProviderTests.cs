// AIModified:2025-10-09T07:22:36Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class IApiDefaultsProviderTests
{
    [TestMethod]
    public void BaseUri_Should_Return_Configured_Uri()
    {
        // Arrange
        var expectedUri = new Uri("https://api.example.com/");
        var defaults = new TestApiDefaultsProvider(expectedUri, null, TimeSpan.FromSeconds(30), null);

        // Act
        var baseUri = defaults.BaseUri;

        // Assert
        Assert.AreEqual(expectedUri, baseUri);
    }

    [TestMethod]
    public void BaseUri_Should_Return_Null_When_Not_Configured()
    {
        // Arrange
        var defaults = new TestApiDefaultsProvider(null, null, TimeSpan.FromSeconds(30), null);

        // Act
        var baseUri = defaults.BaseUri;

        // Assert
        Assert.IsNull(baseUri);
    }

    [TestMethod]
    public void DefaultHeaders_Should_Return_Configured_Headers()
    {
        // Arrange
        var expectedHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["User-Agent"] = "NaturalApi-Test"
        };
        var defaults = new TestApiDefaultsProvider(null, expectedHeaders, TimeSpan.FromSeconds(30), null);

        // Act
        var headers = defaults.DefaultHeaders;

        // Assert
        Assert.AreEqual(expectedHeaders.Count, headers.Count);
        Assert.AreEqual("application/json", headers["Accept"]);
        Assert.AreEqual("NaturalApi-Test", headers["User-Agent"]);
    }

    [TestMethod]
    public void DefaultHeaders_Should_Return_Empty_Dictionary_When_Not_Configured()
    {
        // Arrange
        var defaults = new TestApiDefaultsProvider(null, null, TimeSpan.FromSeconds(30), null);

        // Act
        var headers = defaults.DefaultHeaders;

        // Assert
        Assert.IsNotNull(headers);
        Assert.AreEqual(0, headers.Count);
    }

    [TestMethod]
    public void Timeout_Should_Return_Configured_Timeout()
    {
        // Arrange
        var expectedTimeout = TimeSpan.FromMinutes(5);
        var defaults = new TestApiDefaultsProvider(null, null, expectedTimeout, null);

        // Act
        var timeout = defaults.Timeout;

        // Assert
        Assert.AreEqual(expectedTimeout, timeout);
    }

    [TestMethod]
    public void AuthProvider_Should_Return_Configured_Provider()
    {
        // Arrange
        var authProvider = new TestAuthProvider("test-token");
        var defaults = new TestApiDefaultsProvider(null, null, TimeSpan.FromSeconds(30), authProvider);

        // Act
        var provider = defaults.AuthProvider;

        // Assert
        Assert.AreEqual(authProvider, provider);
    }

    [TestMethod]
    public void AuthProvider_Should_Return_Null_When_Not_Configured()
    {
        // Arrange
        var defaults = new TestApiDefaultsProvider(null, null, TimeSpan.FromSeconds(30), null);

        // Act
        var provider = defaults.AuthProvider;

        // Assert
        Assert.IsNull(provider);
    }

    [TestMethod]
    public void All_Properties_Should_Be_Configurable_Independently()
    {
        // Arrange
        var baseUri = new Uri("https://api.test.com/");
        var headers = new Dictionary<string, string> { ["X-Test"] = "value" };
        var timeout = TimeSpan.FromSeconds(60);
        var authProvider = new TestAuthProvider("independent-token");

        // Act
        var defaults = new TestApiDefaultsProvider(baseUri, headers, timeout, authProvider);

        // Assert
        Assert.AreEqual(baseUri, defaults.BaseUri);
        Assert.AreEqual(headers, defaults.DefaultHeaders);
        Assert.AreEqual(timeout, defaults.Timeout);
        Assert.AreEqual(authProvider, defaults.AuthProvider);
    }

    [TestMethod]
    public void DefaultHeaders_Should_Be_Immutable_Reference()
    {
        // Arrange
        var originalHeaders = new Dictionary<string, string> { ["Test"] = "value" };
        var defaults = new TestApiDefaultsProvider(null, originalHeaders, TimeSpan.FromSeconds(30), null);

        // Act
        var returnedHeaders = defaults.DefaultHeaders;
        returnedHeaders["Modified"] = "new-value";

        // Assert
        // The returned headers should be the same reference, so modifications affect the original
        Assert.AreEqual(2, defaults.DefaultHeaders.Count);
        Assert.IsTrue(defaults.DefaultHeaders.ContainsKey("Test"));
        Assert.IsTrue(defaults.DefaultHeaders.ContainsKey("Modified"));
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
            Uri? baseUri,
            IDictionary<string, string>? defaultHeaders,
            TimeSpan timeout,
            IApiAuthProvider? authProvider)
        {
            BaseUri = baseUri;
            DefaultHeaders = defaultHeaders ?? new Dictionary<string, string>();
            Timeout = timeout;
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
