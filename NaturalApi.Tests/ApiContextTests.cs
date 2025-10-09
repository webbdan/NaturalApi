using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiContextTests
{
    private MockHttpExecutor _mockExecutor = null!;
    private ApiRequestSpec _baseSpec = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _baseSpec = new ApiRequestSpec(
            "/users",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Constructor_Is_Called_With_Null_Spec()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ApiContext(null!, _mockExecutor));
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Constructor_Is_Called_With_Null_Executor()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ApiContext(_baseSpec, null!));
    }

    [TestMethod]
    public void Should_Add_Header_When_WithHeader_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var key = "Authorization";
        var value = "Bearer token123";

        // Act
        var result = context.WithHeader(key, value);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_WithHeader_Is_Called_With_Null_Key()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.WithHeader(null!, "value"));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_WithHeader_Is_Called_With_Empty_Key()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.WithHeader("", "value"));
    }

    [TestMethod]
    public void Should_Add_Headers_When_WithHeaders_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = context.WithHeaders(headers);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_WithHeaders_Is_Called_With_Null_Headers()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => context.WithHeaders(null!));
    }

    [TestMethod]
    public void Should_Add_Query_Parameter_When_WithQueryParam_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var key = "page";
        var value = 1;

        // Act
        var result = context.WithQueryParam(key, value);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_WithQueryParam_Is_Called_With_Null_Key()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.WithQueryParam(null!, "value"));
    }

    [TestMethod]
    public void Should_Add_Query_Parameters_When_WithQueryParams_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var parameters = new { page = 1, size = 10 };

        // Act
        var result = context.WithQueryParams(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_WithQueryParams_Is_Called_With_Null_Parameters()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => context.WithQueryParams(null!));
    }

    [TestMethod]
    public void Should_Add_Path_Parameter_When_WithPathParam_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var key = "id";
        var value = 123;

        // Act
        var result = context.WithPathParam(key, value);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_WithPathParam_Is_Called_With_Null_Key()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.WithPathParam(null!, "value"));
    }

    [TestMethod]
    public void Should_Add_Path_Parameters_When_WithPathParams_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var parameters = new { id = 123, type = "user" };

        // Act
        var result = context.WithPathParams(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_WithPathParams_Is_Called_With_Null_Parameters()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => context.WithPathParams(null!));
    }

    [TestMethod]
    public void Should_Add_Bearer_Auth_When_UsingAuth_Is_Called_With_Token()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var token = "abc123";

        // Act
        var result = context.UsingAuth(token);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Add_Auth_When_UsingAuth_Is_Called_With_Scheme_And_Token()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var schemeAndToken = "Bearer abc123";

        // Act
        var result = context.UsingAuth(schemeAndToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_UsingAuth_Is_Called_With_Null_Token()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.UsingAuth(null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_UsingAuth_Is_Called_With_Empty_Token()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.UsingAuth(""));
    }

    [TestMethod]
    public void Should_Add_Bearer_Token_When_UsingToken_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var token = "abc123";

        // Act
        var result = context.UsingToken(token);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_UsingToken_Is_Called_With_Null_Token()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.UsingToken(null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_UsingToken_Is_Called_With_Empty_Token()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.UsingToken(""));
    }

    [TestMethod]
    public void Should_Set_Timeout_When_WithTimeout_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var result = context.WithTimeout(timeout);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiContext));
        Assert.AreNotSame(context, result); // Should return new instance
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_WithTimeout_Is_Called_With_Zero_Timeout()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.WithTimeout(TimeSpan.Zero));
    }

    [TestMethod]
    public void Should_Throw_ArgumentException_When_WithTimeout_Is_Called_With_Negative_Timeout()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => context.WithTimeout(TimeSpan.FromSeconds(-1)));
    }

    [TestMethod]
    public void Should_Execute_GET_Request_When_Get_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act
        var result = context.Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }

    [TestMethod]
    public void Should_Execute_DELETE_Request_When_Delete_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act
        var result = context.Delete();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }

    [TestMethod]
    public void Should_Execute_POST_Request_When_Post_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var body = new { name = "John", email = "john@example.com" };

        // Act
        var result = context.Post(body);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }

    [TestMethod]
    public void Should_Execute_POST_Request_With_Null_Body_When_Post_Is_Called_With_Null()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);

        // Act
        var result = context.Post(null);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }

    [TestMethod]
    public void Should_Execute_PUT_Request_When_Put_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var body = new { name = "John", email = "john@example.com" };

        // Act
        var result = context.Put(body);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }

    [TestMethod]
    public void Should_Execute_PATCH_Request_When_Patch_Is_Called()
    {
        // Arrange
        var context = new ApiContext(_baseSpec, _mockExecutor);
        var body = new { name = "John" };

        // Act
        var result = context.Patch(body);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }
}
