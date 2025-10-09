using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class AuthenticationTests
{
    private Api _api = null!;
    private MockHttpExecutor _mockExecutor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _api = new Api(_mockExecutor);
    }

    [TestMethod]
    public void Should_Add_Authorization_Header_When_UsingAuth_With_Token()
    {
        // Arrange
        var token = "abc123";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer abc123", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Add_Authorization_Header_When_UsingAuth_With_Scheme_And_Token()
    {
        // Arrange
        var schemeAndToken = "Bearer xyz789";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(schemeAndToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer xyz789", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Add_Authorization_Header_When_UsingToken()
    {
        // Arrange
        var token = "def456";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingToken(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer def456", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Support_Basic_Authentication_When_UsingAuth_With_Basic_Scheme()
    {
        // Arrange
        var basicAuth = "Basic dXNlcjpwYXNzd29yZA=="; // user:password in base64
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(basicAuth)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Basic dXNlcjpwYXNzd29yZA==", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Support_Custom_Authentication_Scheme_When_UsingAuth()
    {
        // Arrange
        var customAuth = "CustomScheme token123";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(customAuth)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("CustomScheme token123", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Work_With_All_HTTP_Methods_When_Using_Authentication()
    {
        // Arrange
        var token = "test-token";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act & Assert - Test all HTTP methods
        var getResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Get();
        var postResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Post();
        var putResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Put();
        var patchResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Patch();
        var deleteResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Delete();

        // All should have Authorization header
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer test-token", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Combine_Authentication_With_Other_Headers_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var customHeader = "Custom-Header-Value";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithHeader("X-Custom-Header", customHeader)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer test-token", _mockExecutor.LastSpec.Headers["Authorization"]);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("X-Custom-Header"));
        Assert.AreEqual(customHeader, _mockExecutor.LastSpec.Headers["X-Custom-Header"]);
    }

    [TestMethod]
    public void Should_Override_Existing_Authorization_Header_When_UsingAuth_After_WithHeader()
    {
        // Arrange
        var originalToken = "original-token";
        var newToken = "new-token";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithHeader("Authorization", $"Bearer {originalToken}")
            .UsingAuth(newToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer new-token", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Path_Parameters_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var userId = "123";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/users/{id}")
            .WithPathParam("id", userId)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer test-token", _mockExecutor.LastSpec.Headers["Authorization"]);
        // Note: Path parameter replacement happens in the executor, not in the spec
        Assert.AreEqual("https://httpbin.org/users/{id}", _mockExecutor.LastSpec.Endpoint);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Query_Parameters_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var queryParam = "test-value";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithQueryParam("param", queryParam)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer test-token", _mockExecutor.LastSpec.Headers["Authorization"]);
        Assert.IsTrue(_mockExecutor.LastSpec.QueryParams.ContainsKey("param"));
        Assert.AreEqual(queryParam, _mockExecutor.LastSpec.QueryParams["param"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Timeout_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var timeout = TimeSpan.FromSeconds(30);
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithTimeout(timeout)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer test-token", _mockExecutor.LastSpec.Headers["Authorization"]);
        Assert.AreEqual(timeout, _mockExecutor.LastSpec.Timeout);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Special_Characters_In_Token()
    {
        // Arrange
        var tokenWithSpecialChars = "token-with-special-chars!@#$%^&*()";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(tokenWithSpecialChars)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual($"Bearer {tokenWithSpecialChars}", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Unicode_Characters_In_Token()
    {
        // Arrange
        var unicodeToken = "token-with-unicode-测试";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(unicodeToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual($"Bearer {unicodeToken}", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Very_Long_Token()
    {
        // Arrange
        var longToken = new string('a', 1000); // 1000 character token
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(longToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual($"Bearer {longToken}", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Should_Throw_Exception_When_UsingAuth_With_Empty_Token()
    {
        // Arrange
        var emptyToken = "";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act & Assert - Should throw ArgumentException
        _api.For("https://httpbin.org/headers")
            .UsingAuth(emptyToken)
            .Get();
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Complex_Scheme()
    {
        // Arrange
        var complexScheme = "Bearer token-with-multiple-parts-and-special-chars!@#$%";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(complexScheme)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual(complexScheme, _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Multiple_Spaces_In_Scheme()
    {
        // Arrange
        var multiSpaceScheme = "Bearer  token  with  spaces";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(multiSpaceScheme)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual(multiSpaceScheme, _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Quoted_Token()
    {
        // Arrange
        var quotedToken = "\"quoted-token-value\"";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(quotedToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual($"Bearer {quotedToken}", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Base64_Encoded_Token()
    {
        // Arrange
        var base64Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-token"));
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(base64Token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual($"Bearer {base64Token}", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_JWT_Token()
    {
        // Arrange
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(jwtToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual($"Bearer {jwtToken}", _mockExecutor.LastSpec.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Chained_Calls()
    {
        // Arrange
        var token = "chained-token";
        var customHeader = "chained-header-value";
        var queryParam = "chained-param-value";
        _mockExecutor.SetupResponse(200, """{"message":"Mock response"}""");

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithHeader("X-Custom-Header", customHeader)
            .WithQueryParam("param", queryParam)
            .WithTimeout(TimeSpan.FromSeconds(15))
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer chained-token", _mockExecutor.LastSpec.Headers["Authorization"]);
        Assert.IsTrue(_mockExecutor.LastSpec.Headers.ContainsKey("X-Custom-Header"));
        Assert.AreEqual(customHeader, _mockExecutor.LastSpec.Headers["X-Custom-Header"]);
        Assert.IsTrue(_mockExecutor.LastSpec.QueryParams.ContainsKey("param"));
        Assert.AreEqual(queryParam, _mockExecutor.LastSpec.QueryParams["param"]);
        Assert.AreEqual(TimeSpan.FromSeconds(15), _mockExecutor.LastSpec.Timeout);
    }
}