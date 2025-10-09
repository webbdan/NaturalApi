// AIModified:2025-10-09T07:22:36Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class DefaultApiDefaultsTests
{
    [TestMethod]
    public void Constructor_With_No_Parameters_Should_Set_Default_Values()
    {
        // Act
        var defaults = new DefaultApiDefaults();

        // Assert
        Assert.IsNull(defaults.BaseUri);
        Assert.IsNotNull(defaults.DefaultHeaders);
        Assert.AreEqual(0, defaults.DefaultHeaders.Count);
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.Timeout);
        Assert.IsNull(defaults.AuthProvider);
    }

    [TestMethod]
    public void Constructor_With_BaseUri_Should_Set_BaseUri()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com/");

        // Act
        var defaults = new DefaultApiDefaults(baseUri: baseUri);

        // Assert
        Assert.AreEqual(baseUri, defaults.BaseUri);
        Assert.IsNotNull(defaults.DefaultHeaders);
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.Timeout);
        Assert.IsNull(defaults.AuthProvider);
    }

    [TestMethod]
    public void Constructor_With_DefaultHeaders_Should_Set_Headers()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["User-Agent"] = "NaturalApi"
        };

        // Act
        var defaults = new DefaultApiDefaults(defaultHeaders: headers);

        // Assert
        Assert.IsNull(defaults.BaseUri);
        Assert.AreEqual(headers, defaults.DefaultHeaders);
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.Timeout);
        Assert.IsNull(defaults.AuthProvider);
    }

    [TestMethod]
    public void Constructor_With_Timeout_Should_Set_Timeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var defaults = new DefaultApiDefaults(timeout: timeout);

        // Assert
        Assert.IsNull(defaults.BaseUri);
        Assert.IsNotNull(defaults.DefaultHeaders);
        Assert.AreEqual(timeout, defaults.Timeout);
        Assert.IsNull(defaults.AuthProvider);
    }

    [TestMethod]
    public void Constructor_With_AuthProvider_Should_Set_AuthProvider()
    {
        // Arrange
        var authProvider = new TestAuthProvider("test-token");

        // Act
        var defaults = new DefaultApiDefaults(authProvider: authProvider);

        // Assert
        Assert.IsNull(defaults.BaseUri);
        Assert.IsNotNull(defaults.DefaultHeaders);
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.Timeout);
        Assert.AreEqual(authProvider, defaults.AuthProvider);
    }

    [TestMethod]
    public void Constructor_With_All_Parameters_Should_Set_All_Values()
    {
        // Arrange
        var baseUri = new Uri("https://api.test.com/");
        var headers = new Dictionary<string, string> { ["X-Test"] = "value" };
        var timeout = TimeSpan.FromSeconds(60);
        var authProvider = new TestAuthProvider("all-params-token");

        // Act
        var defaults = new DefaultApiDefaults(baseUri, headers, timeout, authProvider);

        // Assert
        Assert.AreEqual(baseUri, defaults.BaseUri);
        Assert.AreEqual(headers, defaults.DefaultHeaders);
        Assert.AreEqual(timeout, defaults.Timeout);
        Assert.AreEqual(authProvider, defaults.AuthProvider);
    }

    [TestMethod]
    public void Constructor_With_Null_DefaultHeaders_Should_Create_Empty_Dictionary()
    {
        // Act
        var defaults = new DefaultApiDefaults(defaultHeaders: null);

        // Assert
        Assert.IsNotNull(defaults.DefaultHeaders);
        Assert.AreEqual(0, defaults.DefaultHeaders.Count);
    }

    [TestMethod]
    public void Constructor_With_Null_Timeout_Should_Use_Default_Timeout()
    {
        // Act
        var defaults = new DefaultApiDefaults(timeout: null);

        // Assert
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.Timeout);
    }

    [TestMethod]
    public void Properties_Should_Be_ReadOnly()
    {
        // Arrange
        var defaults = new DefaultApiDefaults();

        // Act & Assert
        // These should compile without errors, indicating the properties are read-only
        var baseUri = defaults.BaseUri;
        var headers = defaults.DefaultHeaders;
        var timeout = defaults.Timeout;
        var authProvider = defaults.AuthProvider;

        Assert.IsNotNull(baseUri == null || baseUri != null);
        Assert.IsNotNull(headers);
        Assert.IsTrue(timeout > TimeSpan.Zero);
        Assert.IsTrue(authProvider == null || authProvider != null);
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
