# Testing Guide

> NaturalApi is designed to work seamlessly with both unit testing and integration testing. This guide covers testing patterns, mocking strategies, and best practices for testing API interactions.

---

## Table of Contents

- [Unit Testing](#unit-testing)
- [Integration Testing](#integration-testing)
- [WireMock Integration Testing](#wiremock-integration-testing)
- [Mocking Strategies](#mocking-strategies)
- [Test Data Management](#test-data-management)
- [Parallel Testing](#parallel-testing)
- [Testing Authentication](#testing-authentication)
- [Error Scenario Testing](#error-scenario-testing)
- [Best Practices](#best-practices)

---

## Unit Testing

### Basic Unit Test Setup

```csharp
[TestClass]
public class UserServiceTests
{
    private MockHttpExecutor _mockExecutor;
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _api = new Api(_mockExecutor);
    }

    [TestMethod]
    public async Task GetUser_Should_Return_User_When_User_Exists()
    {
        // Arrange
        _mockExecutor.SetupResponse(200, """{"id":1,"name":"John Doe","email":"john@example.com"}""");

        // Act
        var user = await _api.For("/users/1")
            .Get()
            .ShouldReturn<User>();

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual("john@example.com", user.Email);
    }
}
```

### Testing Different HTTP Methods

```csharp
[TestMethod]
public async Task CreateUser_Should_Return_201_When_User_Created()
{
    // Arrange
    var newUser = new { name = "Jane Doe", email = "jane@example.com" };
    _mockExecutor.SetupResponse(201, """{"id":2,"name":"Jane Doe","email":"jane@example.com"}""");

    // Act
    var user = await _api.For("/users")
        .Post(newUser)
        .ShouldReturn<User>(status: 201);

    // Assert
    Assert.AreEqual(2, user.Id);
    Assert.AreEqual("Jane Doe", user.Name);
}

[TestMethod]
public async Task UpdateUser_Should_Return_200_When_User_Updated()
{
    // Arrange
    var updatedUser = new { id = 1, name = "John Updated", email = "john.updated@example.com" };
    _mockExecutor.SetupResponse(200, """{"id":1,"name":"John Updated","email":"john.updated@example.com"}""");

    // Act
    var user = await _api.For("/users/1")
        .Put(updatedUser)
        .ShouldReturn<User>(status: 200);

    // Assert
    Assert.AreEqual("John Updated", user.Name);
}

[TestMethod]
public async Task DeleteUser_Should_Return_204_When_User_Deleted()
{
    // Arrange
    _mockExecutor.SetupResponse(204, "");

    // Act & Assert
    await _api.For("/users/1")
        .Delete()
        .ShouldReturn(status: 204);
}
```

### Testing Request Configuration

```csharp
[TestMethod]
public async Task GetUser_Should_Include_Headers_And_Parameters()
{
    // Arrange
    _mockExecutor.SetupResponse(200, """{"id":1,"name":"John"}""");

    // Act
    await _api.For("/users/1")
        .WithHeader("Accept", "application/json")
        .WithQueryParam("include", "profile")
        .Get()
        .ShouldReturn<User>();

    // Assert
    var spec = _mockExecutor.LastSpec;
    Assert.IsTrue(spec.Headers.ContainsKey("Accept"));
    Assert.AreEqual("application/json", spec.Headers["Accept"]);
    Assert.IsTrue(spec.QueryParams.ContainsKey("include"));
    Assert.AreEqual("profile", spec.QueryParams["include"]);
}
```

### Testing Authentication

```csharp
[TestMethod]
public async Task GetProtectedResource_Should_Include_Authorization_Header()
{
    // Arrange
    _mockExecutor.SetupResponse(200, """{"data":"protected"}""");

    // Act
    await _api.For("/protected")
        .UsingAuth("Bearer token123")
        .Get()
        .ShouldReturn<object>();

    // Assert
    var spec = _mockExecutor.LastSpec;
    Assert.IsTrue(spec.Headers.ContainsKey("Authorization"));
    Assert.AreEqual("Bearer token123", spec.Headers["Authorization"]);
}
```

---

## Integration Testing

### Real API Integration Tests

```csharp
[TestClass]
public class UserApiIntegrationTests
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        _api = new Api(executor);
    }

    [TestMethod]
    public async Task GetUser_Should_Return_User_From_Real_API()
    {
        // Act
        var user = await _api.For("https://jsonplaceholder.typicode.com/users/1")
            .Get()
            .ShouldReturn<User>();

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(1, user.Id);
        Assert.IsNotNull(user.Name);
        Assert.IsNotNull(user.Email);
    }

    [TestMethod]
    public async Task CreateUser_Should_Return_201_From_Real_API()
    {
        // Arrange
        var newUser = new
        {
            name = "Test User",
            email = "test@example.com",
            username = "testuser"
        };

        // Act
        var user = await _api.For("https://jsonplaceholder.typicode.com/users")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(newUser)
            .ShouldReturn<User>(status: 201);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Test User", user.Name);
        Assert.AreEqual("test@example.com", user.Email);
    }
}
```

### Testing with Test Containers

```csharp
[TestClass]
public class ApiIntegrationTests
{
    private IApi _api;
    private TestContainer _container;

    [TestInitialize]
    public async Task Setup()
    {
        _container = new TestContainer();
        await _container.StartAsync();
        
        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        _api = new Api(executor);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _container.StopAsync();
    }

    [TestMethod]
    public async Task GetUsers_Should_Return_Users_From_Test_Container()
    {
        // Act
        var users = await _api.For($"{_container.BaseUrl}/users")
            .Get()
            .ShouldReturn<List<User>>();

        // Assert
        Assert.IsNotNull(users);
        Assert.IsTrue(users.Count > 0);
    }
}
```

---

## WireMock Integration Testing

WireMock is the recommended approach for integration testing with NaturalApi. It provides realistic HTTP server mocking with full request/response control.

### Setting Up WireMock

First, add the WireMock NuGet package:

```bash
dotnet add package WireMock.Net
```

### Basic WireMock Setup

```csharp
using WireMock.Server;
using WireMock.Settings;

[TestClass]
public class WireMockIntegrationTests
{
    private WireMockServer _wireMockServer = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create WireMock server on random port
        _wireMockServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Random port
            StartAdminInterface = false
        });

        // Create NaturalApi instance pointing to WireMock
        var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_wireMockServer.Ports[0]}") };
        _api = new Api(new HttpClientExecutor(httpClient));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _wireMockServer?.Dispose();
    }

    [TestMethod]
    public async Task GetUsers_Should_Return_Mocked_Users()
    {
        // Arrange - Setup WireMock response
        _wireMockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/users")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                [
                    {"id":1,"name":"John Doe","email":"john@example.com"},
                    {"id":2,"name":"Jane Smith","email":"jane@example.com"}
                ]
                """));

        // Act
        var users = await _api.For("/users")
            .Get()
            .ShouldReturn<List<User>>();

        // Assert
        Assert.IsNotNull(users);
        Assert.AreEqual(2, users.Count);
        Assert.AreEqual("John Doe", users[0].Name);
    }
}
```

### Authentication Testing with WireMock

```csharp
[TestClass]
public class AuthenticationIntegrationTests
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        // Configure services with authentication
        var services = new ServiceCollection();
        
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });
        
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Authenticated_Request_Should_Work()
    {
        // Act - Use AsUser() method
        var result = _api.For("/api/protected")
            .AsUser("testuser", "testpass")
            .Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
```

### WireMockServers Helper Class

Create a helper class to manage multiple WireMock servers:

```csharp
public class WireMockServers : IDisposable
{
    public WireMockServer AuthServer { get; }
    public WireMockServer ApiServer { get; }
    
    public string AuthBaseUrl => $"http://localhost:{AuthServer.Ports[0]}";
    public string ApiBaseUrl => $"http://localhost:{ApiServer.Ports[0]}";

    public WireMockServers()
    {
        // Create auth server
        AuthServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = false
        });

        // Create API server
        ApiServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = false
        });

        SetupAuthEndpoints();
        SetupApiEndpoints();
    }

    private void SetupAuthEndpoints()
    {
        // Valid user credentials
        AuthServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/auth/login")
                .UsingPost()
                .WithBody("{\"username\":\"testuser\",\"password\":\"testpass\"}"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"token\":\"valid-token-12345\",\"expiresIn\":600}"));

        // Invalid credentials
        AuthServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/auth/login")
                .UsingPost()
                .WithBody("{\"username\":\"invalid\",\"password\":\"invalid\"}"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"Invalid credentials\"}"));
    }

    private void SetupApiEndpoints()
    {
        // Protected endpoint that validates bearer token
        ApiServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/protected")
                .UsingGet()
                .WithHeader("Authorization", "Bearer valid-token-12345"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"message\":\"Access granted\",\"data\":\"protected-resource-data\"}"));

        // Invalid or missing token
        ApiServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/protected")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"Unauthorized\"}"));
    }

    public void Dispose()
    {
        AuthServer?.Dispose();
        ApiServer?.Dispose();
    }
}
```

### Testing Without Dependency Injection

```csharp
[TestClass]
public class NoDiIntegrationTests
{
    private WireMockServers _wireMockServers = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Ultra_Simple_Usage_Should_Work()
    {
        // Arrange - Ultra simple usage - no base URL needed
        var api = new Api();

        // Act - Use absolute URL directly
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task With_Base_Url_And_Auth_Should_Work()
    {
        // Arrange - Create API with base URL and auth provider
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) };
        var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        var api = new Api(defaults, new HttpClient { BaseAddress = new Uri(_wireMockServers.ApiBaseUrl) });

        // Act - Use relative endpoint with authentication
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
```

### Testing Configuration Patterns

```csharp
[TestClass]
public class ConfigurationIntegrationTests
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Configuration_From_AppSettings_Should_Work()
    {
        // Arrange - Create configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl,
                ["ApiSettings:AuthEndpoint"] = "/auth/login"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Configure HttpClient for auth service
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use configuration to register NaturalApi
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
```

### Advanced WireMock Patterns

```csharp
[TestClass]
public class AdvancedWireMockTests
{
    private WireMockServer _server = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _server = WireMockServer.Start();
        _api = new Api(new HttpClientExecutor(new HttpClient { BaseAddress = new Uri(_server.Urls[0]) }));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _server?.Dispose();
    }

    [TestMethod]
    public async Task Should_Handle_Dynamic_Responses()
    {
        // Arrange - Setup dynamic response based on request
        _server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/users")
                .UsingGet()
                .WithParam("id", "1"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"id\":1,\"name\":\"User 1\"}"));

        _server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/users")
                .UsingGet()
                .WithParam("id", "2"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"id\":2,\"name\":\"User 2\"}"));

        // Act
        var user1 = await _api.For("/users?id=1").Get().ShouldReturn<User>();
        var user2 = await _api.For("/users?id=2").Get().ShouldReturn<User>();

        // Assert
        Assert.AreEqual("User 1", user1.Name);
        Assert.AreEqual("User 2", user2.Name);
    }

    [TestMethod]
    public async Task Should_Verify_Request_Details()
    {
        // Arrange
        _server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/users")
                .UsingPost())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(201)
                .WithBody("{\"id\":1,\"name\":\"Created User\"}"));

        // Act
        var newUser = new { name = "Test User", email = "test@example.com" };
        await _api.For("/users")
            .WithHeaders(new Dictionary<string, string> { ["Content-Type"] = "application/json" })
            .Post(newUser)
            .ShouldReturn<User>(status: 201);

        // Assert - Verify the request was made correctly
        var requests = _server.LogEntries;
        Assert.AreEqual(1, requests.Count());
        
        var request = requests.First();
        Assert.AreEqual("POST", request.RequestMessage.Method);
        Assert.AreEqual("/users", request.RequestMessage.Path);
        Assert.IsTrue(request.RequestMessage.Body.Contains("Test User"));
    }
}
```

### WireMock Best Practices

1. **Use Random Ports**: Always use `Port = 0` to avoid port conflicts
2. **Clean Up Resources**: Always dispose of WireMock servers in `[TestCleanup]`
3. **Setup Realistic Responses**: Make your mock responses match real API behavior
4. **Test Request Verification**: Use WireMock's request logging to verify request details
5. **Use Helper Classes**: Create reusable WireMock setup classes for common scenarios

---

## Mocking Strategies

### Custom Mock Executor

```csharp
public class CustomMockHttpExecutor : IHttpExecutor
{
    private readonly Dictionary<string, MockResponse> _responses = new();

    public void SetupResponse(string endpoint, int statusCode, object responseBody, IDictionary<string, string>? headers = null)
    {
        var json = JsonSerializer.Serialize(responseBody);
        _responses[endpoint] = new MockResponse(statusCode, json, headers ?? new Dictionary<string, string>());
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        var endpoint = spec.Endpoint;
        
        if (_responses.TryGetValue(endpoint, out var response))
        {
            return CreateMockResult(response);
        }
        
        throw new InvalidOperationException($"No mock response configured for {endpoint}");
    }

    private IApiResultContext CreateMockResult(MockResponse response)
    {
        var httpResponse = new HttpResponseMessage((HttpStatusCode)response.StatusCode)
        {
            Content = new StringContent(response.Body)
        };

        foreach (var header in response.Headers)
        {
            httpResponse.Headers.Add(header.Key, header.Value);
        }

        return new ApiResultContext(httpResponse, new MockHttpExecutor());
    }
}

public class MockResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
```

### Scenario-Based Mocking

```csharp
[TestClass]
public class UserServiceScenarioTests
{
    private CustomMockHttpExecutor _mockExecutor;
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new CustomMockHttpExecutor();
        _api = new Api(_mockExecutor);
    }

    [TestMethod]
    public async Task GetUser_Should_Handle_404_Scenario()
    {
        // Arrange
        _mockExecutor.SetupResponse("/users/999", 404, new { error = "User not found" });

        // Act & Assert
        try
        {
            await _api.For("/users/999")
                .Get()
                .ShouldReturn<User>(status: 200);
            Assert.Fail("Expected exception was not thrown");
        }
        catch (ApiAssertionException ex)
        {
            Assert.AreEqual(404, ex.ActualStatusCode);
            Assert.IsTrue(ex.Message.Contains("Expected status 200 but got 404"));
        }
    }

    [TestMethod]
    public async Task GetUser_Should_Handle_Server_Error_Scenario()
    {
        // Arrange
        _mockExecutor.SetupResponse("/users/1", 500, new { error = "Internal server error" });

        // Act & Assert
        try
        {
            await _api.For("/users/1")
                .Get()
                .ShouldReturn<User>();
            Assert.Fail("Expected exception was not thrown");
        }
        catch (ApiExecutionException ex)
        {
            Assert.AreEqual(500, ex.StatusCode);
        }
    }
}
```

### Mock with Authentication

```csharp
[TestMethod]
public async Task GetProtectedResource_Should_Use_Auth_Provider()
{
    // Arrange
    var authProvider = new Mock<IApiAuthProvider>();
    authProvider.Setup(x => x.GetAuthTokenAsync(null))
        .ReturnsAsync("mock-token");

    var defaults = new DefaultApiDefaults(authProvider: authProvider.Object);
    var api = new Api(_mockExecutor, defaults);

    _mockExecutor.SetupResponse(200, """{"data":"protected"}""");

    // Act
    await api.For("/protected")
        .Get()
        .ShouldReturn<object>();

    // Assert
    var spec = _mockExecutor.LastSpec;
    Assert.IsTrue(spec.Headers.ContainsKey("Authorization"));
    Assert.AreEqual("Bearer mock-token", spec.Headers["Authorization"]);
}
```

---

## Test Data Management

### Test Data Builders

```csharp
public class UserBuilder
{
    private int _id = 1;
    private string _name = "Test User";
    private string _email = "test@example.com";
    private bool _active = true;

    public UserBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder Inactive()
    {
        _active = false;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Name = _name,
            Email = _email,
            Active = _active
        };
    }

    public object BuildAnonymous()
    {
        return new
        {
            id = _id,
            name = _name,
            email = _email,
            active = _active
        };
    }
}

// Usage in tests
[TestMethod]
public async Task CreateUser_Should_Return_User_With_Generated_Id()
{
    // Arrange
    var userData = new UserBuilder()
        .WithName("John Doe")
        .WithEmail("john@example.com")
        .BuildAnonymous();

    var expectedUser = new UserBuilder()
        .WithId(123)
        .WithName("John Doe")
        .WithEmail("john@example.com")
        .Build();

    _mockExecutor.SetupResponse(201, JsonSerializer.Serialize(expectedUser));

    // Act
    var user = await _api.For("/users")
        .Post(userData)
        .ShouldReturn<User>(status: 201);

    // Assert
    Assert.AreEqual(123, user.Id);
    Assert.AreEqual("John Doe", user.Name);
}
```

### Test Data Factories

```csharp
public static class TestDataFactory
{
    public static User CreateUser(int id = 1, string? name = null, string? email = null)
    {
        return new User
        {
            Id = id,
            Name = name ?? $"User {id}",
            Email = email ?? $"user{id}@example.com",
            Active = true
        };
    }

    public static List<User> CreateUsers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateUser(i))
            .ToList();
    }

    public static object CreateUserRequest(string? name = null, string? email = null)
    {
        return new
        {
            name = name ?? "Test User",
            email = email ?? "test@example.com"
        };
    }
}

// Usage
[TestMethod]
public async Task GetUsers_Should_Return_List_Of_Users()
{
    // Arrange
    var users = TestDataFactory.CreateUsers(5);
    _mockExecutor.SetupResponse(200, JsonSerializer.Serialize(users));

    // Act
    var result = await _api.For("/users")
        .Get()
        .ShouldReturn<List<User>>();

    // Assert
    Assert.AreEqual(5, result.Count);
}
```

---

## Parallel Testing

### Thread-Safe Testing

```csharp
[TestClass]
public class ParallelApiTests
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        _api = new Api(executor);
    }

    [TestMethod]
    public async Task Multiple_Concurrent_Requests_Should_Work()
    {
        // Arrange
        var tasks = new List<Task<User>>();

        // Act - Make 10 concurrent requests
        for (int i = 1; i <= 10; i++)
        {
            tasks.Add(_api.For($"https://jsonplaceholder.typicode.com/users/{i}")
                .Get()
                .ShouldReturn<User>());
        }

        var users = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10, users.Length);
        Assert.IsTrue(users.All(u => u.Id > 0));
    }

    [TestMethod]
    public async Task Parallel_Post_Requests_Should_Work()
    {
        // Arrange
        var tasks = new List<Task<User>>();

        // Act - Create 5 users concurrently
        for (int i = 1; i <= 5; i++)
        {
            var userData = new { name = $"User {i}", email = $"user{i}@example.com" };
            tasks.Add(_api.For("https://jsonplaceholder.typicode.com/users")
                .Post(userData)
                .ShouldReturn<User>());
        }

        var users = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(5, users.Length);
    }
}
```

### Test Isolation

```csharp
[TestClass]
public class IsolatedApiTests
{
    [TestMethod]
    public async Task Test_Should_Not_Interfere_With_Other_Tests()
    {
        // Each test gets its own API instance
        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        var api = new Api(executor);

        // Test implementation
        var user = await api.For("https://jsonplaceholder.typicode.com/users/1")
            .Get()
            .ShouldReturn<User>();

        Assert.IsNotNull(user);
    }

    [TestMethod]
    public async Task Another_Test_Should_Be_Independent()
    {
        // This test is completely independent
        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        var api = new Api(executor);

        // Different test implementation
        var posts = await api.For("https://jsonplaceholder.typicode.com/posts")
            .Get()
            .ShouldReturn<List<Post>>();

        Assert.IsNotNull(posts);
    }
}
```

---

## Testing Authentication

### Testing Auth Providers

```csharp
[TestClass]
public class AuthProviderTests
{
    [TestMethod]
    public async Task CachingAuthProvider_Should_Cache_Token()
    {
        // Arrange
        var authProvider = new CachingAuthProvider();
        
        // Act
        var token1 = await authProvider.GetAuthTokenAsync();
        var token2 = await authProvider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual(token1, token2);
    }

    [TestMethod]
    public async Task MultiUserAuthProvider_Should_Return_Different_Tokens()
    {
        // Arrange
        var authProvider = new MultiUserAuthProvider();

        // Act
        var token1 = await authProvider.GetAuthTokenAsync("user1");
        var token2 = await authProvider.GetAuthTokenAsync("user2");

        // Assert
        Assert.AreNotEqual(token1, token2);
    }
}
```

### Testing Authenticated Endpoints

```csharp
[TestMethod]
public async Task GetProtectedResource_Should_Include_Auth_Header()
{
    // Arrange
    var authProvider = new Mock<IApiAuthProvider>();
    authProvider.Setup(x => x.GetAuthTokenAsync(null))
        .ReturnsAsync("test-token");

    var defaults = new DefaultApiDefaults(authProvider: authProvider.Object);
    var mockExecutor = new MockHttpExecutor();
    var api = new Api(mockExecutor, defaults);

    mockExecutor.SetupResponse(200, """{"data":"protected"}""");

    // Act
    await api.For("/protected")
        .Get()
        .ShouldReturn<object>();

    // Assert
    var spec = mockExecutor.LastSpec;
    Assert.IsTrue(spec.Headers.ContainsKey("Authorization"));
    Assert.AreEqual("Bearer test-token", spec.Headers["Authorization"]);
}
```

---

## Error Scenario Testing

### Testing HTTP Error Status Codes

```csharp
[TestMethod]
public async Task GetUser_Should_Handle_404_Error()
{
    // Arrange
    _mockExecutor.SetupResponse(404, """{"error":"User not found"}""");

    // Act & Assert
    try
    {
        await _api.For("/users/999")
            .Get()
            .ShouldReturn<User>(status: 200);
        Assert.Fail("Expected ApiAssertionException");
    }
    catch (ApiAssertionException ex)
    {
        Assert.AreEqual(404, ex.ActualStatusCode);
        Assert.IsTrue(ex.Message.Contains("Expected status 200 but got 404"));
    }
}

[TestMethod]
public async Task GetUser_Should_Handle_500_Error()
{
    // Arrange
    _mockExecutor.SetupResponse(500, """{"error":"Internal server error"}""");

    // Act & Assert
    try
    {
        await _api.For("/users/1")
            .Get()
            .ShouldReturn<User>();
        Assert.Fail("Expected ApiExecutionException");
    }
    catch (ApiExecutionException ex)
    {
        Assert.AreEqual(500, ex.StatusCode);
    }
}
```

### Testing Timeout Scenarios

```csharp
[TestMethod]
public async Task GetData_Should_Handle_Timeout()
{
    // Arrange
    var timeoutExecutor = new MockTimeoutHttpExecutor();
    var api = new Api(timeoutExecutor);

    // Act & Assert
    try
    {
        await api.For("/slow-endpoint")
            .WithTimeout(TimeSpan.FromMilliseconds(100))
            .Get()
            .ShouldReturn<Data>();
        Assert.Fail("Expected timeout exception");
    }
    catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
    {
        // Expected timeout
        Assert.IsTrue(true);
    }
}
```

### Testing Validation Failures

```csharp
[TestMethod]
public async Task GetUser_Should_Validate_Response_Body()
{
    // Arrange
    _mockExecutor.SetupResponse(200, """{"id":1,"name":"John","email":"invalid-email"}""");

    // Act & Assert
    try
    {
        await _api.For("/users/1")
            .Get()
            .ShouldReturn<User>(body: u => u.Email.Contains("@"));
        Assert.Fail("Expected validation failure");
    }
    catch (ApiAssertionException ex)
    {
        Assert.IsTrue(ex.Message.Contains("body validation failed"));
    }
}
```

---

## Best Practices

### 1. Use Descriptive Test Names

```csharp
[TestMethod]
public async Task GetUser_Should_Return_User_When_Valid_Id_Provided()
{
    // Test implementation
}

[TestMethod]
public async Task GetUser_Should_Throw_404_When_User_Does_Not_Exist()
{
    // Test implementation
}
```

### 2. Arrange-Act-Assert Pattern

```csharp
[TestMethod]
public async Task CreateUser_Should_Return_201_When_Valid_Data_Provided()
{
    // Arrange
    var userData = new { name = "John Doe", email = "john@example.com" };
    _mockExecutor.SetupResponse(201, """{"id":1,"name":"John Doe","email":"john@example.com"}""");

    // Act
    var user = await _api.For("/users")
        .Post(userData)
        .ShouldReturn<User>(status: 201);

    // Assert
    Assert.AreEqual(1, user.Id);
    Assert.AreEqual("John Doe", user.Name);
    Assert.AreEqual("john@example.com", user.Email);
}
```

### 3. Test One Thing at a Time

```csharp
[TestMethod]
public async Task GetUser_Should_Return_Correct_User_Data()
{
    // Test only data retrieval
}

[TestMethod]
public async Task GetUser_Should_Include_Correct_Headers()
{
    // Test only header handling
}

[TestMethod]
public async Task GetUser_Should_Handle_Authentication()
{
    // Test only authentication
}
```

### 4. Use Test Categories

```csharp
[TestMethod]
[TestCategory("Unit")]
public async Task GetUser_Should_Return_User_From_Mock()
{
    // Unit test
}

[TestMethod]
[TestCategory("Integration")]
public async Task GetUser_Should_Return_User_From_Real_API()
{
    // Integration test
}
```

### 5. Clean Up Resources

```csharp
[TestClass]
public class ApiTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IApi _api;

    public ApiTests()
    {
        _httpClient = new HttpClient();
        var executor = new HttpClientExecutor(_httpClient);
        _api = new Api(executor);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
```

### 6. Use Test Data Builders

```csharp
[TestMethod]
public async Task CreateUser_Should_Return_User_With_Generated_Id()
{
    // Arrange
    var userData = new UserBuilder()
        .WithName("John Doe")
        .WithEmail("john@example.com")
        .BuildAnonymous();

    var expectedUser = new UserBuilder()
        .WithId(123)
        .WithName("John Doe")
        .WithEmail("john@example.com")
        .Build();

    _mockExecutor.SetupResponse(201, JsonSerializer.Serialize(expectedUser));

    // Act
    var user = await _api.For("/users")
        .Post(userData)
        .ShouldReturn<User>(status: 201);

    // Assert
    Assert.AreEqual(123, user.Id);
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic testing concepts
- **[Error Handling](error-handling.md)** - Testing error scenarios and exceptions
- **[Authentication](authentication.md)** - Testing authentication patterns
- **[Assertions](assertions.md)** - Response validation in tests
- **[Examples](examples.md)** - Real-world testing scenarios
- **[Troubleshooting](troubleshooting.md)** - Common testing issues
- **[Configuration](configuration.md)** - Test configuration and setup
- **[Request Building](request-building.md)** - Testing request configuration
- **[HTTP Verbs](http-verbs.md)** - Testing different HTTP methods
- **[API Reference](api-reference.md)** - MockHttpExecutor documentation
