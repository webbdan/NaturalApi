// AIModified:2025-10-09T07:52:56Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Collections.Generic;

namespace NaturalApi.Tests;

[TestClass]
public class ApiResponseTests
{
    [TestMethod]
    public void ShouldReturn_With_ApiResponse_Should_Return_Wrapped_Response()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        // Mock response with headers
        mockExecutor.SetupResponse(200, 
            """{"name": "Dan", "id": 123}""", 
            new Dictionary<string, string> { ["x-correlation-id"] = "abc123" });

        // Act
        var resp = api.For("/users/me").Get().ShouldReturn<ApiResponse<UserResponse>>();

        // Assert
        Assert.IsNotNull(resp.Body);
        Assert.AreEqual("Dan", resp.Body.Name);
        Assert.AreEqual(123, resp.Body.Id);
        Assert.AreEqual(200, resp.StatusCode);
        Assert.AreEqual("abc123", resp.Headers["x-correlation-id"]);
    }

    [TestMethod]
    public void ShouldReturn_With_Direct_Type_Should_Return_Body_Only()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        mockExecutor.SetupResponse(200, """{"name": "Dan", "id": 123}""");

        // Act
        var user = api.For("/users/me").Get().ShouldReturn<UserResponse>();

        // Assert
        Assert.AreEqual("Dan", user.Name);
        Assert.AreEqual(123, user.Id);
        // No access to StatusCode or Headers - this is the body only
    }

    [TestMethod]
    public void ShouldReturn_ApiResponse_Should_Allow_Header_Access()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        mockExecutor.SetupResponse(200, """{"name": "Dan"}""", 
            new Dictionary<string, string> { ["x-correlation-id"] = "test123" });

        // Act
        var resp = api.For("/users/me").Get().ShouldReturn<ApiResponse<UserResponse>>();

        // Assert
        Assert.IsTrue(resp.Headers.ContainsKey("x-correlation-id"));
        Assert.AreEqual("test123", resp.Headers["x-correlation-id"]);
    }

    [TestMethod]
    public void ShouldReturn_ApiResponse_Should_Allow_StatusCode_Access()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        mockExecutor.SetupResponse(201, """{"name": "Dan"}""");

        // Act
        var resp = api.For("/users/me").Get().ShouldReturn<ApiResponse<UserResponse>>();

        // Assert
        Assert.AreEqual(201, resp.StatusCode);
        Assert.AreEqual("Dan", resp.Body.Name);
    }

    [TestMethod]
    public void ShouldReturn_ApiResponse_Should_Allow_RawBody_Access()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        var jsonBody = """{"name": "Dan", "id": 123}""";
        mockExecutor.SetupResponse(200, jsonBody);

        // Act
        var resp = api.For("/users/me").Get().ShouldReturn<ApiResponse<UserResponse>>();

        // Assert
        Assert.AreEqual(jsonBody, resp.RawBody);
        Assert.AreEqual("Dan", resp.Body.Name);
    }
}

public class UserResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }
}
