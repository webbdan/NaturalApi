using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiRequestSpecTests
{
    private ApiRequestSpec _baseSpec = null!;

    [TestInitialize]
    public void Setup()
    {
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
    public void Should_Create_New_Spec_With_Header_When_WithHeader_Is_Called()
    {
        // Arrange
        var key = "Authorization";
        var value = "Bearer token123";

        // Act
        var result = _baseSpec.WithHeader(key, value);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(_baseSpec.Endpoint, result.Endpoint);
        Assert.AreEqual(_baseSpec.Method, result.Method);
        Assert.AreEqual(1, result.Headers.Count);
        Assert.AreEqual(value, result.Headers[key]);
    }

    [TestMethod]
    public void Should_Add_Multiple_Headers_When_WithHeaders_Is_Called()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = _baseSpec.WithHeaders(headers);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(2, result.Headers.Count);
        Assert.AreEqual("Bearer token123", result.Headers["Authorization"]);
        Assert.AreEqual("application/json", result.Headers["Content-Type"]);
    }

    [TestMethod]
    public void Should_Overwrite_Existing_Header_When_WithHeader_Is_Called_With_Same_Key()
    {
        // Arrange
        var specWithHeader = _baseSpec.WithHeader("Authorization", "Bearer token123");
        var newValue = "Bearer newtoken456";

        // Act
        var result = specWithHeader.WithHeader("Authorization", newValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Headers.Count);
        Assert.AreEqual(newValue, result.Headers["Authorization"]);
    }

    [TestMethod]
    public void Should_Create_New_Spec_With_Query_Parameter_When_WithQueryParam_Is_Called()
    {
        // Arrange
        var key = "page";
        var value = 1;

        // Act
        var result = _baseSpec.WithQueryParam(key, value);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(_baseSpec.Endpoint, result.Endpoint);
        Assert.AreEqual(_baseSpec.Method, result.Method);
        Assert.AreEqual(1, result.QueryParams.Count);
        Assert.AreEqual(value, result.QueryParams[key]);
    }

    [TestMethod]
    public void Should_Add_Query_Parameters_From_Object_When_WithQueryParams_Is_Called()
    {
        // Arrange
        var parameters = new { page = 1, size = 10, sort = "name" };

        // Act
        var result = _baseSpec.WithQueryParams(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(3, result.QueryParams.Count);
        Assert.AreEqual(1, result.QueryParams["page"]);
        Assert.AreEqual(10, result.QueryParams["size"]);
        Assert.AreEqual("name", result.QueryParams["sort"]);
    }

    [TestMethod]
    public void Should_Add_Query_Parameters_From_Dictionary_When_WithQueryParams_Is_Called()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10
        };

        // Act
        var result = _baseSpec.WithQueryParams(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(2, result.QueryParams.Count);
        Assert.AreEqual(1, result.QueryParams["page"]);
        Assert.AreEqual(10, result.QueryParams["size"]);
    }

    [TestMethod]
    public void Should_Overwrite_Existing_Query_Parameter_When_WithQueryParam_Is_Called_With_Same_Key()
    {
        // Arrange
        var specWithParam = _baseSpec.WithQueryParam("page", 1);
        var newValue = 2;

        // Act
        var result = specWithParam.WithQueryParam("page", newValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.QueryParams.Count);
        Assert.AreEqual(newValue, result.QueryParams["page"]);
    }

    [TestMethod]
    public void Should_Create_New_Spec_With_Path_Parameter_When_WithPathParam_Is_Called()
    {
        // Arrange
        var key = "id";
        var value = 123;

        // Act
        var result = _baseSpec.WithPathParam(key, value);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(_baseSpec.Endpoint, result.Endpoint);
        Assert.AreEqual(_baseSpec.Method, result.Method);
        Assert.AreEqual(1, result.PathParams.Count);
        Assert.AreEqual(value, result.PathParams[key]);
    }

    [TestMethod]
    public void Should_Add_Path_Parameters_From_Object_When_WithPathParams_Is_Called()
    {
        // Arrange
        var parameters = new { id = 123, type = "user" };

        // Act
        var result = _baseSpec.WithPathParams(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(2, result.PathParams.Count);
        Assert.AreEqual(123, result.PathParams["id"]);
        Assert.AreEqual("user", result.PathParams["type"]);
    }

    [TestMethod]
    public void Should_Add_Path_Parameters_From_Dictionary_When_WithPathParams_Is_Called()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["id"] = 123,
            ["type"] = "user"
        };

        // Act
        var result = _baseSpec.WithPathParams(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(2, result.PathParams.Count);
        Assert.AreEqual(123, result.PathParams["id"]);
        Assert.AreEqual("user", result.PathParams["type"]);
    }

    [TestMethod]
    public void Should_Overwrite_Existing_Path_Parameter_When_WithPathParam_Is_Called_With_Same_Key()
    {
        // Arrange
        var specWithParam = _baseSpec.WithPathParam("id", 123);
        var newValue = 456;

        // Act
        var result = specWithParam.WithPathParam("id", newValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.PathParams.Count);
        Assert.AreEqual(newValue, result.PathParams["id"]);
    }

    [TestMethod]
    public void Should_Create_New_Spec_With_Different_Method_When_WithMethod_Is_Called()
    {
        // Arrange
        var newMethod = HttpMethod.Post;

        // Act
        var result = _baseSpec.WithMethod(newMethod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(_baseSpec.Endpoint, result.Endpoint);
        Assert.AreEqual(newMethod, result.Method);
        Assert.AreEqual(_baseSpec.Headers, result.Headers);
        Assert.AreEqual(_baseSpec.QueryParams, result.QueryParams);
        Assert.AreEqual(_baseSpec.PathParams, result.PathParams);
        Assert.AreEqual(_baseSpec.Body, result.Body);
        Assert.AreEqual(_baseSpec.Timeout, result.Timeout);
    }

    [TestMethod]
    public void Should_Create_New_Spec_With_Body_When_WithBody_Is_Called()
    {
        // Arrange
        var body = new { name = "John", email = "john@example.com" };

        // Act
        var result = _baseSpec.WithBody(body);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(_baseSpec.Endpoint, result.Endpoint);
        Assert.AreEqual(_baseSpec.Method, result.Method);
        Assert.AreEqual(body, result.Body);
    }

    [TestMethod]
    public void Should_Create_New_Spec_With_Null_Body_When_WithBody_Is_Called_With_Null()
    {
        // Act
        var result = _baseSpec.WithBody(null);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.IsNull(result.Body);
    }

    [TestMethod]
    public void Should_Create_New_Spec_With_Timeout_When_WithTimeout_Is_Called()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var result = _baseSpec.WithTimeout(timeout);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotSame(_baseSpec, result); // Should return new instance
        Assert.AreEqual(_baseSpec.Endpoint, result.Endpoint);
        Assert.AreEqual(_baseSpec.Method, result.Method);
        Assert.AreEqual(timeout, result.Timeout);
    }

    [TestMethod]
    public void Should_Preserve_All_Properties_When_Creating_New_Spec()
    {
        // Arrange
        var originalSpec = new ApiRequestSpec(
            "/users/{id}",
            HttpMethod.Post,
            new Dictionary<string, string> { ["Authorization"] = "Bearer token" },
            new Dictionary<string, object> { ["page"] = 1 },
            new Dictionary<string, object> { ["id"] = 123 },
            new { name = "John" },
            TimeSpan.FromSeconds(30));

        // Act
        var result = originalSpec.WithHeader("Content-Type", "application/json");

        // Assert
        Assert.AreEqual("/users/{id}", result.Endpoint);
        Assert.AreEqual(HttpMethod.Post, result.Method);
        Assert.AreEqual(2, result.Headers.Count);
        Assert.AreEqual("Bearer token", result.Headers["Authorization"]);
        Assert.AreEqual("application/json", result.Headers["Content-Type"]);
        Assert.AreEqual(1, result.QueryParams.Count);
        Assert.AreEqual(1, result.QueryParams.Count);
        Assert.AreEqual(1, result.PathParams.Count);
        Assert.IsNotNull(result.Body);
        Assert.AreEqual(TimeSpan.FromSeconds(30), result.Timeout);
    }
}
