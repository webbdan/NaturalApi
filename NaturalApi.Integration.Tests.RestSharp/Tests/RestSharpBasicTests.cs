using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using NaturalApi.Integration.Tests.RestSharp.Executors;
using NaturalApi.Integration.Tests.RestSharp.Common;

namespace NaturalApi.Integration.Tests.RestSharp.Tests;

/// <summary>
/// Tests proving RestSharp executor handles all HTTP operations correctly.
/// </summary>
[TestClass]
public class RestSharpBasicTests
{
    private ServiceProvider? _serviceProvider;
    private WireMockServers? _wireMockServers;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        _wireMockServers.Start();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
        _wireMockServers?.Stop();
    }

    [TestMethod]
    public void RestSharpExecutor_GetRequest_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[{\"id\":1,\"name\":\"John\"}]}");

        // Act & Assert
        var result = api.For("/users").Get();
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[{\"id\":1,\"name\":\"John\"}]}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_PostRequest_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        var userData = new { name = "John Doe", email = "john@example.com" };
        _wireMockServers.SetupPost("/users", 201, "{\"id\":1,\"name\":\"John Doe\",\"email\":\"john@example.com\"}");

        // Act & Assert
        var result = api.For("/users").Post(userData);
        Assert.AreEqual(201, result.StatusCode);
        Assert.AreEqual("{\"id\":1,\"name\":\"John Doe\",\"email\":\"john@example.com\"}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_PutRequest_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        var userData = new { name = "Jane Doe", email = "jane@example.com" };
        _wireMockServers.SetupPut("/users/1", 200, "{\"id\":1,\"name\":\"Jane Doe\",\"email\":\"jane@example.com\"}");

        // Act & Assert
        var result = api.For("/users/1").Put(userData);
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"id\":1,\"name\":\"Jane Doe\",\"email\":\"jane@example.com\"}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_PatchRequest_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        var userData = new { name = "Jane Smith" };
        _wireMockServers.SetupPatch("/users/1", 200, "{\"id\":1,\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}");

        // Act & Assert
        var result = api.For("/users/1").Patch(userData);
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"id\":1,\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_DeleteRequest_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupDelete("/users/1", 204, "");

        // Act & Assert
        var result = api.For("/users/1").Delete();
        Assert.AreEqual(204, result.StatusCode);
        Assert.AreEqual("", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_WithHeaders_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[]}");

        // Act & Assert
        var result = api.For("/users")
            .WithHeader("Accept", "application/json")
            .WithHeader("User-Agent", "RestSharp-Test")
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_WithQueryParams_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[],\"page\":1,\"limit\":10}");

        // Act & Assert
        var result = api.For("/users")
            .WithQueryParam("page", 1)
            .WithQueryParam("limit", 10)
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[],\"page\":1,\"limit\":10}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_WithPathParams_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users/123", 200, "{\"id\":123,\"name\":\"John\"}");

        // Act & Assert
        var result = api.For("/users/{id}")
            .WithPathParam("id", 123)
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"id\":123,\"name\":\"John\"}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_WithCookies_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[]}");

        // Act & Assert
        var result = api.For("/users")
            .WithCookie("sessionId", "abc123")
            .WithCookie("userId", "456")
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_WithTimeout_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[]}");

        // Act & Assert
        var result = api.For("/users")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .Get();
        
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("{\"users\":[]}", result.RawBody);
    }

    [TestMethod]
    public void RestSharpExecutor_DeserializeResponse_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users/1", 200, "{\"id\":1,\"name\":\"John Doe\",\"email\":\"john@example.com\"}");

        // Act & Assert
        var result = api.For("/users/1").Get();
        Assert.AreEqual(200, result.StatusCode);
        var user = result.BodyAs<User>();
        
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual("john@example.com", user.Email);
    }

    [TestMethod]
    public void RestSharpExecutor_ShouldReturnValidation_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 200, "{\"users\":[{\"id\":1,\"name\":\"John\"}]}");

        // Act & Assert
        var result = api.For("/users").Get();
        Assert.AreEqual(200, result.StatusCode);
        
        result.ShouldReturn<UserList>(200, users => users.Users.Count == 1);
        result.ShouldReturn<UserList>(users => users.Users[0].Name == "John");
    }

    [TestMethod]
    public void RestSharpExecutor_ErrorHandling_WorksCorrectly()
    {
        // Arrange
        var services = RestSharpTestHelpers.CreateServiceCollection(_wireMockServers!.BaseUrl);
        _serviceProvider = services.BuildServiceProvider();
        var api = _serviceProvider.GetRequiredService<IApi>();

        _wireMockServers.SetupGet("/users", 404, "{\"error\":\"Not Found\"}");

        // Act & Assert
        var result = api.For("/users").Get();
        
        Assert.AreEqual(404, result.StatusCode);
        Assert.AreEqual("{\"error\":\"Not Found\"}", result.RawBody);
    }



}

/// <summary>
/// Test user model.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Test user list model.
/// </summary>
public class UserList
{
    public List<User> Users { get; set; } = new();
}

