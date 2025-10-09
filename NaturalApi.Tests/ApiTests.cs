using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiTests
{
    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Constructor_Is_Called_With_Null_Executor()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api((IHttpExecutor)null!));
    }

    [TestMethod]
    public void Should_Return_ApiContext_When_For_Is_Called_With_Valid_Endpoint()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        var endpoint = "/users";

        // Act
        var result = api.For(endpoint);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Null_Endpoint()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => api.For(null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Empty_Endpoint()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => api.For(""));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Whitespace_Endpoint()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => api.For("   "));
    }

    [TestMethod]
    public void Should_Return_ApiContext_When_For_Is_Called_With_Relative_Endpoint()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        var endpoint = "users/123";

        // Act
        var result = api.For(endpoint);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Return_ApiContext_When_For_Is_Called_With_Absolute_Endpoint()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        var endpoint = "https://api.example.com/users";

        // Act
        var result = api.For(endpoint);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }
}
