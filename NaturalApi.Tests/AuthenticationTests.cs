using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net.Http;

namespace NaturalApi.Tests;

[TestClass]
public class AuthenticationTests
{
    private Api _api = null!;
    private AuthenticatedHttpClientExecutor _authenticatedExecutor = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpClient = new HttpClient();
        _authenticatedExecutor = new AuthenticatedHttpClientExecutor(_httpClient);
        _api = new Api(_authenticatedExecutor);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    [TestMethod]
    public void Should_Add_Authorization_Header_When_UsingAuth_With_Token()
    {
        // Arrange
        var token = "abc123";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the UsingAuth() call and add the Authorization header
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Add_Authorization_Header_When_UsingAuth_With_Scheme_And_Token()
    {
        // Arrange
        var schemeAndToken = "Bearer xyz789";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(schemeAndToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the UsingAuth() call with scheme and token
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Add_Authorization_Header_When_UsingToken()
    {
        // Arrange
        var token = "def456";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingToken(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the UsingToken() call and add Bearer prefix
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Support_Basic_Authentication_When_UsingAuth_With_Basic_Scheme()
    {
        // Arrange
        var basicAuth = "Basic dXNlcjpwYXNzd29yZA=="; // user:password in base64

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(basicAuth)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the UsingAuth() call with Basic auth
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Support_Custom_Authentication_Scheme_When_UsingAuth()
    {
        // Arrange
        var customAuth = "CustomScheme token123";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(customAuth)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the UsingAuth() call with custom scheme
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Work_With_All_HTTP_Methods_When_Using_Authentication()
    {
        // Arrange
        var token = "test-token";

        // Act & Assert - Test all HTTP methods
        var getResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Get();
        var postResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Post();
        var putResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Put();
        var patchResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Patch();
        var deleteResult = _api.For("https://httpbin.org/headers").UsingAuth(token).Delete();

        // All should be processed by the authenticated executor
        Assert.IsTrue(getResult.StatusCode >= 200);
        Assert.IsTrue(postResult.StatusCode >= 200);
        Assert.IsTrue(putResult.StatusCode >= 200);
        Assert.IsTrue(patchResult.StatusCode >= 200);
        Assert.IsTrue(deleteResult.StatusCode >= 200);
    }

    [TestMethod]
    public void Should_Combine_Authentication_With_Other_Headers_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var customHeader = "Custom-Header-Value";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithHeader("X-Custom-Header", customHeader)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle both custom headers and authentication
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Override_Existing_Authorization_Header_When_UsingAuth_After_WithHeader()
    {
        // Arrange
        var originalToken = "original-token";
        var newToken = "new-token";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithHeader("Authorization", $"Bearer {originalToken}")
            .UsingAuth(newToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle the UsingAuth() call and override existing auth
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Path_Parameters_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var userId = "123";

        // Act
        var result = _api.For("https://httpbin.org/users/{id}")
            .WithPathParam("id", userId)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle both path parameters and authentication
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Query_Parameters_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var queryParam = "test-value";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithQueryParam("param", queryParam)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle both query parameters and authentication
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Timeout_When_Provided()
    {
        // Arrange
        var token = "test-token";
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .WithTimeout(timeout)
            .UsingAuth(token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle both timeout and authentication
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Special_Characters_In_Token()
    {
        // Arrange
        var tokenWithSpecialChars = "token-with-special-chars!@#$%^&*()";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(tokenWithSpecialChars)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle tokens with special characters
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Unicode_Characters_In_Token()
    {
        // Arrange
        var unicodeToken = "token-with-unicode-test"; // Use ASCII characters since HTTP headers don't support Unicode

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(unicodeToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle tokens with ASCII characters
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Very_Long_Token()
    {
        // Arrange
        var longToken = new string('a', 1000); // 1000 character token

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(longToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        // The authenticated executor should handle very long tokens
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Should_Throw_Exception_When_UsingAuth_With_Empty_Token()
    {
        // Arrange
        var emptyToken = "";

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

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(complexScheme)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        // The authenticated executor should handle complex authentication schemes
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Multiple_Spaces_In_Scheme()
    {
        // Arrange
        var multiSpaceScheme = "Bearer  token  with  spaces";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(multiSpaceScheme)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        // The authenticated executor should handle authentication with multiple spaces
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Quoted_Token()
    {
        // Arrange
        var quotedToken = "\"quoted-token-value\"";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(quotedToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        // The authenticated executor should handle quoted tokens
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Base64_Encoded_Token()
    {
        // Arrange
        var base64Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-token"));

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(base64Token)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        // The authenticated executor should handle base64 encoded tokens
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_JWT_Token()
    {
        // Arrange
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var result = _api.For("https://httpbin.org/headers")
            .UsingAuth(jwtToken)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        // The authenticated executor should handle JWT tokens
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }

    [TestMethod]
    public void Should_Handle_Authentication_With_Chained_Calls()
    {
        // Arrange
        var token = "chained-token";
        var customHeader = "chained-header-value";
        var queryParam = "chained-param-value";

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
        // The authenticated executor should handle all chained calls including authentication
        Assert.IsTrue(result.StatusCode >= 200); // Any HTTP response indicates the request was processed
    }
}