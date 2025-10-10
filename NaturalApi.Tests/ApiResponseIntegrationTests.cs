using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Collections.Generic;

namespace NaturalApi.Tests;

[TestClass]
public class ApiResponseIntegrationTests
{
    [TestMethod]
    public void Original_Code_Example_Should_Work()
    {
        // This test demonstrates the exact code example provided by the user
        // var resp = await api
        //     .For("/users/me")
        //     .Get()
        //     .ShouldReturn<ApiResponse<UserResponse>>();
        //
        // resp.Body.Name.ShouldBe("Dan");
        // resp.Headers["x-correlation-id"].ShouldNotBeNull();

        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        // Mock response with headers
        mockExecutor.SetupResponse(200, 
            """{"name": "Dan", "id": 123}""", 
            new Dictionary<string, string> { ["x-correlation-id"] = "abc123" });

        // Act - This is the exact pattern the user wanted
        var resp = api.For("/users/me").Get().ShouldReturn<ApiResponse<UserResponse>>();

        // Assert - This demonstrates the exact usage pattern
        Assert.AreEqual("Dan", resp.Body.Name);
        Assert.AreEqual(123, resp.Body.Id);
        Assert.AreEqual(200, resp.StatusCode);
        Assert.IsNotNull(resp.Headers["x-correlation-id"]);
        Assert.AreEqual("abc123", resp.Headers["x-correlation-id"]);
    }

    [TestMethod]
    public void Both_Patterns_Should_Work()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        mockExecutor.SetupResponse(200, """{"name": "Dan", "id": 123}""");

        // Act - Test both patterns work
        var directUser = api.For("/users/me").Get().ShouldReturn<UserResponse>();
        var wrappedResponse = api.For("/users/me").Get().ShouldReturn<ApiResponse<UserResponse>>();

        // Assert - Both should work
        Assert.AreEqual("Dan", directUser.Name);
        Assert.AreEqual("Dan", wrappedResponse.Body.Name);
        Assert.AreEqual(123, directUser.Id);
        Assert.AreEqual(123, wrappedResponse.Body.Id);
        
        // Wrapped response has additional metadata
        Assert.AreEqual(200, wrappedResponse.StatusCode);
        Assert.IsNotNull(wrappedResponse.Headers);
        Assert.IsNotNull(wrappedResponse.RawBody);
    }

    [TestMethod]
    public void Should_Support_Complex_Response_Structures()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var api = new Api(mockExecutor);
        
        var complexJson = """{"user": {"name": "Dan", "id": 123}, "metadata": {"version": "1.0"}}""";
        mockExecutor.SetupResponse(200, complexJson, 
            new Dictionary<string, string> { ["x-correlation-id"] = "test123" });

        // Act
        var resp = api.For("/users/me").Get().ShouldReturn<ApiResponse<ComplexResponse>>();

        // Assert
        Assert.AreEqual("Dan", resp.Body.User.Name);
        Assert.AreEqual(123, resp.Body.User.Id);
        Assert.AreEqual("1.0", resp.Body.Metadata.Version);
        Assert.AreEqual(200, resp.StatusCode);
        Assert.AreEqual("test123", resp.Headers["x-correlation-id"]);
    }
}

public class ComplexResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public UserResponse User { get; set; } = new();
    
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public MetadataResponse Metadata { get; set; } = new();
}

public class MetadataResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
