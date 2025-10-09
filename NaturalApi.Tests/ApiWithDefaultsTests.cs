// AIModified:2025-10-09T07:22:36Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiWithDefaultsTests
{
    private MockHttpExecutor _mockExecutor = null!;
    private TestApiDefaultsProvider _defaults = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _defaults = new TestApiDefaultsProvider();
    }

    [TestMethod]
    public void Constructor_With_Defaults_Should_Set_Defaults()
    {
        // Act
        var api = new Api(_mockExecutor, _defaults);

        // Assert
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void Constructor_With_Null_Executor_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api(null!, _defaults));
    }

    [TestMethod]
    public void Constructor_With_Null_Defaults_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api(_mockExecutor, null!));
    }

    [TestMethod]
    public void For_Should_Use_Defaults_BaseUri()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);
    }

    [TestMethod]
    public void For_Should_Use_Defaults_Headers()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Accept"));
        Assert.AreEqual("application/json", _mockExecutor.LastSpec.Headers["Accept"]);
    }

    [TestMethod]
    public void For_Should_Use_Defaults_Timeout()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TimeSpan.FromSeconds(30), _mockExecutor.LastSpec.Timeout);
    }

    [TestMethod]
    public void For_Should_Override_Defaults_With_Explicit_Values()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .WithHeader("Accept", "application/xml")
            .WithTimeout(TimeSpan.FromSeconds(60))
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("application/xml", _mockExecutor.LastSpec.Headers["Accept"]);
        Assert.AreEqual(TimeSpan.FromSeconds(60), _mockExecutor.LastSpec.Timeout);
    }

    [TestMethod]
    public void For_Should_Handle_Absolute_URLs_Without_Defaults()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("https://external-api.com/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://external-api.com/test", _mockExecutor.LastSpec.Endpoint);
    }

    [TestMethod]
    public void For_Should_Combine_Defaults_With_Additional_Headers()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Act
        var result = api.For("/test")
            .WithHeader("Custom-Header", "custom-value")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Accept"));
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Custom-Header"));
        Assert.AreEqual("application/json", _mockExecutor.LastSpec.Headers["Accept"]);
        Assert.AreEqual("custom-value", _mockExecutor.LastSpec.Headers["Custom-Header"]);
    }

    [TestMethod]
    public void For_Should_Work_With_All_HTTP_Methods()
    {
        // Arrange
        var api = new Api(_mockExecutor, _defaults);

        // Test GET
        var getResult = api.For("/test").Get();
        Assert.IsNotNull(getResult);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);

        // Test POST
        var postResult = api.For("/test").Post(new { data = "test" });
        Assert.IsNotNull(postResult);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);

        // Test PUT
        var putResult = api.For("/test").Put(new { data = "test" });
        Assert.IsNotNull(putResult);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);

        // Test PATCH
        var patchResult = api.For("/test").Patch(new { data = "test" });
        Assert.IsNotNull(patchResult);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);

        // Test DELETE
        var deleteResult = api.For("/test").Delete();
        Assert.IsNotNull(deleteResult);
        Assert.AreEqual("https://api.example.com/test", _mockExecutor.LastSpec.Endpoint);
    }

    [TestMethod]
    public void For_Should_Handle_Empty_Defaults()
    {
        // Arrange
        var emptyDefaults = new EmptyTestApiDefaultsProvider();
        var api = new Api(_mockExecutor, emptyDefaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("/test", _mockExecutor.LastSpec.Endpoint);
        Assert.AreEqual(0, _mockExecutor.LastSpec.Headers.Count);
        Assert.AreEqual(TimeSpan.FromSeconds(1), _mockExecutor.LastSpec.Timeout);
    }

    [TestMethod]
    public void For_Should_Handle_Null_Defaults_Properties()
    {
        // Arrange
        var nullDefaults = new EmptyTestApiDefaultsProvider();
        var api = new Api(_mockExecutor, nullDefaults);

        // Act
        var result = api.For("/test").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("/test", _mockExecutor.LastSpec.Endpoint);
        Assert.AreEqual(0, _mockExecutor.LastSpec.Headers.Count);
        Assert.AreEqual(TimeSpan.FromSeconds(1), _mockExecutor.LastSpec.Timeout);
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
    /// Empty test implementation of IApiDefaultsProvider for testing null/empty scenarios.
    /// </summary>
    private class EmptyTestApiDefaultsProvider : IApiDefaultsProvider
    {
        public Uri? BaseUri => null;
        public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
        public TimeSpan Timeout => TimeSpan.FromSeconds(1); // Use a small timeout instead of zero
        public IApiAuthProvider? AuthProvider => null;
    }
}
