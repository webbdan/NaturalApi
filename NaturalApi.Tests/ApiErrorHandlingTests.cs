using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiErrorHandlingTests
{
    private MockHttpExecutor _mockExecutor = null!;
    private Api _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _api = new Api(_mockExecutor);
    }

    #region Constructor Error Handling

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Constructor_Is_Called_With_Null_Executor()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api((IHttpExecutor)null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_BaseUrl_Constructor_Is_Called_With_Null_BaseUrl()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api((string)null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_BaseUrl_Constructor_Is_Called_With_Empty_BaseUrl()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api(""));
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_BaseUrl_Constructor_Is_Called_With_Whitespace_BaseUrl()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Api("   "));
    }

    #endregion

    #region For() Method Error Handling

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Null_Endpoint()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For(null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Empty_Endpoint()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For(""));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Whitespace_Endpoint()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For("   "));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Only_Slashes()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For("///"));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Only_Spaces()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For("   "));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Only_Tabs()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For("\t\t\t"));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_For_Is_Called_With_Only_Newlines()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _api.For("\n\n\n"));
    }

    #endregion

    #region Valid URI Scenarios

    [TestMethod]
    public void Should_Accept_Valid_Relative_Endpoint()
    {
        // Act
        var result = _api.For("users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Relative_Endpoint_With_Slash()
    {
        // Act
        var result = _api.For("/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Relative_Endpoint_With_Multiple_Segments()
    {
        // Act
        var result = _api.For("api/v1/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Absolute_Http_Endpoint()
    {
        // Act
        var result = _api.For("http://api.example.com/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Absolute_Https_Endpoint()
    {
        // Act
        var result = _api.For("https://api.example.com/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Endpoint_With_Query_Parameters()
    {
        // Act
        var result = _api.For("https://api.example.com/users?page=1&limit=10");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Endpoint_With_Fragment()
    {
        // Act
        var result = _api.For("https://api.example.com/users#section1");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Endpoint_With_Port()
    {
        // Act
        var result = _api.For("https://api.example.com:8080/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Endpoint_With_Subdomain()
    {
        // Act
        var result = _api.For("https://v1.api.example.com/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Endpoint_With_International_Domain()
    {
        // Act
        var result = _api.For("https://api.测试.com/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Accept_Valid_Endpoint_With_Unicode_Path()
    {
        // Act
        var result = _api.For("https://api.example.com/用户/123");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    #endregion

    #region Base URL Constructor Tests

    [TestMethod]
    public void Should_Combine_BaseUrl_With_Relative_Endpoint()
    {
        // Arrange
        var api = new Api("https://api.example.com");

        // Act
        var result = api.For("users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Combine_BaseUrl_With_Relative_Endpoint_With_Slash()
    {
        // Arrange
        var api = new Api("https://api.example.com");

        // Act
        var result = api.For("/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Not_Modify_Absolute_Endpoint_When_BaseUrl_Is_Provided()
    {
        // Arrange
        var api = new Api("https://api.example.com");

        // Act
        var result = api.For("https://other-api.example.com/users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_BaseUrl_With_Trailing_Slash()
    {
        // Arrange
        var api = new Api("https://api.example.com/");

        // Act
        var result = api.For("users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_BaseUrl_With_Path()
    {
        // Arrange
        var api = new Api("https://api.example.com/v1");

        // Act
        var result = api.For("users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_BaseUrl_With_Path_And_Trailing_Slash()
    {
        // Arrange
        var api = new Api("https://api.example.com/v1/");

        // Act
        var result = api.For("users");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Should_Handle_Endpoint_With_Only_Slash()
    {
        // Act
        var result = _api.For("/");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_Endpoint_With_Multiple_Slashes()
    {
        // Act
        var result = _api.For("//users//");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_Endpoint_With_Special_Characters()
    {
        // Act
        var result = _api.For("users/123?filter=name&sort=asc");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_Endpoint_With_Encoded_Characters()
    {
        // Act
        var result = _api.For("users/123%20test");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_Endpoint_With_Query_Parameters_And_Fragment()
    {
        // Act
        var result = _api.For("https://api.example.com/users?page=1#top");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    [TestMethod]
    public void Should_Handle_Endpoint_With_Complex_Path()
    {
        // Act
        var result = _api.For("api/v1/users/123/orders/456/items");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
    }

    #endregion

    #region Error Message Validation

    [TestMethod]
    public void Should_Provide_Clear_Error_Message_For_Null_Endpoint()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => _api.For(null!));
        Assert.IsTrue(exception.Message.Contains("Endpoint cannot be null or empty"));
        Assert.AreEqual("endpoint", exception.ParamName);
    }

    [TestMethod]
    public void Should_Provide_Clear_Error_Message_For_Empty_Endpoint()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => _api.For(""));
        Assert.IsTrue(exception.Message.Contains("Endpoint cannot be null or empty"));
        Assert.AreEqual("endpoint", exception.ParamName);
    }

    [TestMethod]
    public void Should_Provide_Clear_Error_Message_For_Whitespace_Endpoint()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => _api.For("   "));
        Assert.IsTrue(exception.Message.Contains("Endpoint cannot be null or empty"));
        Assert.AreEqual("endpoint", exception.ParamName);
    }

    [TestMethod]
    public void Should_Provide_Clear_Error_Message_For_Null_Executor()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => new Api((IHttpExecutor)null!));
        Assert.AreEqual("httpExecutor", exception.ParamName);
    }

    [TestMethod]
    public void Should_Provide_Clear_Error_Message_For_Null_BaseUrl()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => new Api((string)null!));
        Assert.AreEqual("baseUrl", exception.ParamName);
    }

    #endregion
}
