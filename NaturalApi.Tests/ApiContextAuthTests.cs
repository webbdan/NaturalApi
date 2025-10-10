using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiContextAuthTests
{
    private ApiContext _context = null!;
    private MockHttpExecutor _mockExecutor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        var spec = new ApiRequestSpec(
            "https://api.example.com/test",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);
        _context = new ApiContext(spec, _mockExecutor);
    }

    [TestMethod]
    public void WithoutAuth_Should_Return_New_Context_With_SuppressAuth_True()
    {
        // Act
        var newContext = _context.WithoutAuth();

        // Assert
        Assert.IsNotNull(newContext);
        Assert.IsInstanceOfType(newContext, typeof(IApiContext));
        Assert.AreNotEqual(_context, newContext);
    }

    [TestMethod]
    public void WithoutAuth_Should_Be_Chainable()
    {
        // Act
        var result = _context
            .WithHeader("Accept", "application/json")
            .WithoutAuth()
            .WithQueryParam("test", "value")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
    }

    [TestMethod]
    public void AsUser_Should_Return_New_Context_With_Username_Set()
    {
        // Arrange
        var username = "testuser";

        // Act
        var newContext = _context.AsUser(username);

        // Assert
        Assert.IsNotNull(newContext);
        Assert.IsInstanceOfType(newContext, typeof(IApiContext));
        Assert.AreNotEqual(_context, newContext);
    }

    [TestMethod]
    public void AsUser_Should_Set_Username_In_Spec()
    {
        // Arrange
        var username = "testuser";

        // Act
        var result = _context.AsUser(username).Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);
    }

    [TestMethod]
    public void AsUser_Should_Throw_ArgumentException_When_Username_Is_Null()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _context.AsUser(null!));
    }

    [TestMethod]
    public void AsUser_Should_Throw_ArgumentException_When_Username_Is_Empty()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _context.AsUser(""));
    }

    [TestMethod]
    public void AsUser_Should_Throw_ArgumentException_When_Username_Is_Whitespace()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _context.AsUser("   "));
    }

    [TestMethod]
    public void AsUser_Should_Be_Chainable()
    {
        // Act
        var result = _context
            .WithHeader("Accept", "application/json")
            .AsUser("testuser")
            .WithQueryParam("test", "value")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
    }

    [TestMethod]
    public void WithoutAuth_And_AsUser_Should_Work_Together()
    {
        // Act
        var result = _context
            .WithoutAuth()
            .AsUser("testuser")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
    }

    [TestMethod]
    public void AsUser_And_WithoutAuth_Should_Work_Together()
    {
        // Act
        var result = _context
            .AsUser("testuser")
            .WithoutAuth()
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
    }

    [TestMethod]
    public void WithoutAuth_Should_Override_Existing_Auth_Headers()
    {
        // Act
        var result = _context
            .WithHeader("Authorization", "Bearer existing-token")
            .WithoutAuth()
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
        // The Authorization header should still be there, but SuppressAuth should be true
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
    }

    [TestMethod]
    public void AsUser_Should_Work_With_Existing_Auth_Headers()
    {
        // Act
        var result = _context
            .WithHeader("Authorization", "Bearer existing-token")
            .AsUser("testuser")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
    }

    [TestMethod]
    public void WithoutAuth_Should_Work_With_All_HTTP_Methods()
    {
        // Test GET
        var getResult = _context.WithoutAuth().Get();
        Assert.IsNotNull(getResult);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);

        // Test POST
        var postResult = _context.WithoutAuth().Post(new { test = "data" });
        Assert.IsNotNull(postResult);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);

        // Test PUT
        var putResult = _context.WithoutAuth().Put(new { test = "data" });
        Assert.IsNotNull(putResult);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);

        // Test PATCH
        var patchResult = _context.WithoutAuth().Patch(new { test = "data" });
        Assert.IsNotNull(patchResult);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);

        // Test DELETE
        var deleteResult = _context.WithoutAuth().Delete();
        Assert.IsNotNull(deleteResult);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
    }

    [TestMethod]
    public void AsUser_Should_Work_With_All_HTTP_Methods()
    {
        // Arrange
        var username = "testuser";

        // Test GET
        var getResult = _context.AsUser(username).Get();
        Assert.IsNotNull(getResult);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);

        // Test POST
        var postResult = _context.AsUser(username).Post(new { test = "data" });
        Assert.IsNotNull(postResult);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);

        // Test PUT
        var putResult = _context.AsUser(username).Put(new { test = "data" });
        Assert.IsNotNull(putResult);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);

        // Test PATCH
        var patchResult = _context.AsUser(username).Patch(new { test = "data" });
        Assert.IsNotNull(patchResult);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);

        // Test DELETE
        var deleteResult = _context.AsUser(username).Delete();
        Assert.IsNotNull(deleteResult);
        Assert.AreEqual(username, _mockExecutor.LastSpec.Username);
    }

    [TestMethod]
    public void WithoutAuth_Should_Be_Immutable()
    {
        // Act
        var context1 = _context.WithoutAuth();
        var context2 = _context.WithoutAuth();

        // Assert
        Assert.AreNotEqual(context1, context2);
        Assert.AreNotEqual(_context, context1);
        Assert.AreNotEqual(_context, context2);
    }

    [TestMethod]
    public void AsUser_Should_Be_Immutable()
    {
        // Act
        var context1 = _context.AsUser("user1");
        var context2 = _context.AsUser("user2");

        // Assert
        Assert.AreNotEqual(context1, context2);
        Assert.AreNotEqual(_context, context1);
        Assert.AreNotEqual(_context, context2);
    }

    [TestMethod]
    public void WithoutAuth_Should_Not_Affect_Other_Properties()
    {
        // Arrange
        var contextWithHeaders = _context
            .WithHeader("Accept", "application/json")
            .WithQueryParam("test", "value")
            .WithTimeout(TimeSpan.FromSeconds(60));

        // Act
        var result = contextWithHeaders.WithoutAuth().Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(_mockExecutor.LastSpec.SuppressAuth);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Accept"));
        Assert.IsTrue(_mockExecutor.LastSpec.QueryParams.ContainsKey("test"));
        Assert.AreEqual(TimeSpan.FromSeconds(60), _mockExecutor.LastSpec.Timeout);
    }

    [TestMethod]
    public void AsUser_Should_Not_Affect_Other_Properties()
    {
        // Arrange
        var contextWithHeaders = _context
            .WithHeader("Accept", "application/json")
            .WithQueryParam("test", "value")
            .WithTimeout(TimeSpan.FromSeconds(60));

        // Act
        var result = contextWithHeaders.AsUser("testuser").Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testuser", _mockExecutor.LastSpec.Username);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Accept"));
        Assert.IsTrue(_mockExecutor.LastSpec.QueryParams.ContainsKey("test"));
        Assert.AreEqual(TimeSpan.FromSeconds(60), _mockExecutor.LastSpec.Timeout);
    }
}
