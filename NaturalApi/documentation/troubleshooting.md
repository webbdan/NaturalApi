# Troubleshooting Guide

> Common issues, solutions, and debugging techniques for NaturalApi.

---

## Table of Contents

- [Common Setup Issues](#common-setup-issues)
- [Authentication Problems](#authentication-problems)
- [AsUser() Authentication Issues](#asuser-authentication-issues)
- [DI Pattern Selection](#di-pattern-selection)
- [Base URL vs Absolute URLs](#base-url-vs-absolute-urls)
- [WireMock Setup Issues](#wiremock-setup-issues)
- [Deserialization Failures](#deserialization-failures)
- [Timeout Issues](#timeout-issues)
- [Base URL Resolution](#base-url-resolution)
- [DI Registration Issues](#di-registration-issues)
- [Mock Setup Problems](#mock-setup-problems)
- [Performance Issues](#performance-issues)
- [Debugging Techniques](#debugging-techniques)

---

## Common Setup Issues

### Issue: "Api class not found" or "IApi not registered"

**Symptoms:**
- Compilation errors about missing types
- Runtime errors about unregistered services

**Solutions:**

1. **Check Package Installation**
   ```bash
   dotnet add package NaturalApi
   ```

2. **Verify Using Statement**
   ```csharp
   using NaturalApi;
   ```

3. **Check DI Registration**
   ```csharp
   // In Program.cs or Startup.cs
   services.AddNaturalApi();
   ```

4. **Manual Registration**
   ```csharp
   services.AddSingleton<IApi>(provider =>
   {
       var httpClient = provider.GetRequiredService<HttpClient>();
       return new Api(new HttpClientExecutor(httpClient));
   });
   ```

### Issue: "HttpClient not registered"

**Symptoms:**
- `InvalidOperationException: No service for type 'HttpClient'`

**Solutions:**

1. **Register HttpClient**
   ```csharp
   services.AddHttpClient();
   ```

2. **Use HttpClientFactory**
   ```csharp
   services.AddHttpClient<IApi>(client =>
   {
       client.BaseAddress = new Uri("https://api.example.com");
   });
   ```

3. **Manual HttpClient Creation**
   ```csharp
   var httpClient = new HttpClient();
   var api = new Api(new HttpClientExecutor(httpClient));
   ```

---

## Authentication Problems

### Issue: "Authorization header not added"

**Symptoms:**
- Requests fail with 401 Unauthorized
- No Authorization header in request

**Solutions:**

1. **Check Auth Provider Registration**
   ```csharp
   services.AddNaturalApiWithAuth<MyAuthProvider>(new MyAuthProvider());
   ```

2. **Use Inline Authentication**
   ```csharp
   var data = await api.For("/protected")
       .UsingAuth("Bearer your-token")
       .Get()
       .ShouldReturn<Data>();
   ```

3. **Verify Auth Provider Implementation**
   ```csharp
   public class MyAuthProvider : IApiAuthProvider
   {
       public async Task<string?> GetAuthTokenAsync(string? username = null)
       {
           // Return actual token, not null
           return "your-actual-token";
       }
   }
   ```

### Issue: "Token refresh not working"

**Symptoms:**
- Authentication works initially but fails after token expires
- 401 errors after some time

**Solutions:**

1. **Implement Token Caching**
   ```csharp
   public class CachingAuthProvider : IApiAuthProvider
   {
       private string? _token;
       private DateTime _expires;

       public async Task<string?> GetAuthTokenAsync(string? username = null)
       {
           if (_token == null || DateTime.UtcNow > _expires)
           {
               await RefreshTokenAsync();
           }
           return _token;
       }

       private async Task RefreshTokenAsync()
       {
           // Implement token refresh logic
           _token = await GetNewTokenAsync();
           _expires = DateTime.UtcNow.AddMinutes(30);
       }
   }
   ```

2. **Check Token Expiry**
   ```csharp
   public async Task<string?> GetAuthTokenAsync(string? username = null)
   {
       var token = await GetTokenFromCacheAsync();
       if (IsTokenExpired(token))
       {
           token = await RefreshTokenAsync();
       }
       return token;
   }
   ```

### Issue: "Per-user authentication not working"

**Symptoms:**
- All users get the same token
- User-specific requests fail

**Solutions:**

1. **Implement Username Parameter**
   ```csharp
   public async Task<string?> GetAuthTokenAsync(string? username = null)
   {
       if (string.IsNullOrEmpty(username))
           return await GetSystemTokenAsync();
       
       return await GetUserTokenAsync(username);
   }
   ```

2. **Use AsUser() Method**
   ```csharp
   var userData = await api.For("/users/me")
       .AsUser("john.doe")
       .Get()
       .ShouldReturn<UserData>();
   ```

---

## AsUser() Authentication Issues

### Issue: "AsUser() not working" or "Authentication failed"

**Symptoms:**
- `AsUser()` method not found
- Authentication always fails with 401
- "No auth provider configured" errors

**Solutions:**

1. **Check Auth Provider Registration**
   ```csharp
   // Make sure you have an auth provider that supports username/password
   services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
       "https://api.example.com",
       new SimpleCustomAuthProvider(
           new HttpClient { BaseAddress = new Uri("https://auth.example.com") },
           "/auth/login")));
   ```

2. **Verify Auth Provider Implementation**
   ```csharp
   public class SimpleCustomAuthProvider : IApiAuthProvider
   {
       public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
       {
           // Make sure this method accepts both username and password
           if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
           {
               return null;
           }
           
           // Your authentication logic here
           return await AuthenticateAsync(username, password);
       }
   }
   ```

3. **Check WireMock Setup for Tests**
   ```csharp
   // Make sure your WireMock server is configured for the auth endpoint
   _wireMockServer
       .Given(WireMock.RequestBuilders.Request.Create()
           .WithPath("/auth/login")
           .UsingPost()
           .WithBody("{\"username\":\"testuser\",\"password\":\"testpass\"}"))
       .RespondWith(WireMock.ResponseBuilders.Response.Create()
           .WithStatusCode(200)
           .WithBody("{\"token\":\"valid-token\",\"expiresIn\":600}"));
   ```

4. **Debug Authentication Flow**
   ```csharp
   // Add logging to see what's happening
   var result = await api.For("/protected").AsUser("testuser", "testpass").Get();
   Console.WriteLine($"Status: {result.StatusCode}");
   Console.WriteLine($"Body: {result.RawBody}");
   ```

### Issue: "Username/password authentication not supported"

**Symptoms:**
- Auth provider doesn't accept username/password parameters
- `GetAuthTokenAsync` method signature mismatch

**Solutions:**

1. **Update Auth Provider Interface**
   ```csharp
   // Old interface (username only)
   public interface IApiAuthProvider
   {
       Task<string?> GetAuthTokenAsync(string? username = null);
   }

   // New interface (username and password)
   public interface IApiAuthProvider
   {
       Task<string?> GetAuthTokenAsync(string? username = null, string? password = null);
   }
   ```

2. **Implement Username/Password Support**
   ```csharp
   public class MyAuthProvider : IApiAuthProvider
   {
       public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
       {
           if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
           {
               return null;
           }

           // Call your authentication service
           var response = await _httpClient.PostAsync("/auth/login", 
               new StringContent(JsonSerializer.Serialize(new { username, password })));
           
           if (response.IsSuccessStatusCode)
           {
               var content = await response.Content.ReadAsStringAsync();
               var authResponse = JsonSerializer.Deserialize<AuthResponse>(content);
               return authResponse?.Token;
           }
           
           return null;
       }
   }
   ```

---

## DI Pattern Selection

### Issue: "Which DI pattern should I use?"

**Symptoms:**
- Confusion about which registration method to use
- Multiple patterns available but unclear which to choose

**Solutions:**

1. **Use the Decision Tree**
   ```
   Do you need authentication?
   ├─ No → Pattern 1 (Ultra Simple) or Pattern 2 (With Base URL)
   └─ Yes
      ├─ Want relative URLs? → Pattern 4 (With Base URL and Auth) ⭐ RECOMMENDED
      ├─ Need configuration? → Pattern 6 (With Configuration)
      ├─ Need custom control? → Pattern 7 (With Factory)
      └─ Need custom API? → Pattern 8 (With Custom API)
   ```

2. **Start Simple, Evolve as Needed**
   ```csharp
   // Start with Pattern 1 (Ultra Simple)
   services.AddNaturalApi();

   // Add base URL when needed (Pattern 2)
   services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl("https://api.example.com"));

   // Add authentication when needed (Pattern 4)
   services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
       "https://api.example.com", 
       myAuthProvider));
   ```

3. **Common Patterns by Use Case**
   ```csharp
   // Quick prototyping
   services.AddNaturalApi();

   // Most applications
   services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
       "https://api.example.com", 
       myAuthProvider));

   // Multiple environments
   services.AddNaturalApi(provider => 
   {
       var config = provider.GetRequiredService<IConfiguration>();
       var baseUrl = config["ApiSettings:BaseUrl"];
       return new Api(baseUrl, myAuthProvider);
   });
   ```

---

## Base URL vs Absolute URLs

### Issue: "When to use base URL vs absolute URLs?"

**Symptoms:**
- Confusion about when to set a base URL
- Errors when mixing relative and absolute URLs

**Solutions:**

1. **Use Base URL When:**
   - Calling the same API repeatedly
   - Want clean relative URLs (`/users` instead of `https://api.example.com/users`)
   - Have a consistent API endpoint

   ```csharp
   // Set base URL once
   var api = new Api("https://api.example.com");
   
   // Use relative URLs
   var users = await api.For("/users").Get().ShouldReturn<List<User>>();
   var user = await api.For("/users/1").Get().ShouldReturn<User>();
   ```

2. **Use Absolute URLs When:**
   - Calling different APIs
   - No consistent base URL
   - Quick prototyping

   ```csharp
   // No base URL needed
   var api = new Api();
   
   // Use absolute URLs
   var users = await api.For("https://api.example.com/users").Get().ShouldReturn<List<User>>();
   var orders = await api.For("https://orders.example.com/orders").Get().ShouldReturn<List<Order>>();
   ```

3. **Mixed Approach**
   ```csharp
   // Multiple APIs with different base URLs
   services.AddNaturalApi("UserApi", NaturalApiConfiguration.WithBaseUrl("https://users.example.com"));
   services.AddNaturalApi("OrderApi", NaturalApiConfiguration.WithBaseUrl("https://orders.example.com"));
   
   // Use in services
   public class OrderService
   {
       private readonly IApi _userApi;
       private readonly IApi _orderApi;
       
       public OrderService(
           [FromKeyedServices("UserApi")] IApi userApi,
           [FromKeyedServices("OrderApi")] IApi orderApi)
       {
           _userApi = userApi;
           _orderApi = orderApi;
       }
   }
   ```

---

## WireMock Setup Issues

### Issue: "WireMock server not starting" or "Port conflicts"

**Symptoms:**
- `WireMockServer.Start()` fails
- Port already in use errors
- Tests fail to connect to WireMock

**Solutions:**

1. **Use Random Ports**
   ```csharp
   [TestInitialize]
   public void Setup()
   {
       // Always use Port = 0 for random port
       _wireMockServer = WireMockServer.Start(new WireMockServerSettings
       {
           Port = 0, // Random port
           StartAdminInterface = false
       });
   }
   ```

2. **Proper Cleanup**
   ```csharp
   [TestCleanup]
   public void Cleanup()
   {
       _wireMockServer?.Dispose();
   }
   ```

3. **Check Server Status**
   ```csharp
   [TestInitialize]
   public void Setup()
   {
       _wireMockServer = WireMockServer.Start();
       
       // Verify server is running
       Assert.IsTrue(_wireMockServer.IsStarted);
       Assert.IsTrue(_wireMockServer.Ports.Length > 0);
   }
   ```

### Issue: "WireMock responses not matching requests"

**Symptoms:**
- Requests not matching WireMock expectations
- 404 responses from WireMock
- Wrong response data

**Solutions:**

1. **Check Request Matching**
   ```csharp
   // Be specific about request matching
   _wireMockServer
       .Given(WireMock.RequestBuilders.Request.Create()
           .WithPath("/api/users")
           .UsingGet()
           .WithHeader("Authorization", "Bearer valid-token"))
       .RespondWith(WireMock.ResponseBuilders.Response.Create()
           .WithStatusCode(200)
           .WithBody("[{\"id\":1,\"name\":\"John\"}]"));
   ```

2. **Debug Request Details**
   ```csharp
   [TestCleanup]
   public void DebugRequests()
   {
       var requests = _wireMockServer.LogEntries;
       foreach (var request in requests)
       {
           Console.WriteLine($"Method: {request.RequestMessage.Method}");
           Console.WriteLine($"Path: {request.RequestMessage.Path}");
           Console.WriteLine($"Headers: {string.Join(", ", request.RequestMessage.Headers)}");
           Console.WriteLine($"Body: {request.RequestMessage.Body}");
       }
   }
   ```

3. **Use Flexible Matching**
   ```csharp
   // More flexible matching
   _wireMockServer
       .Given(WireMock.RequestBuilders.Request.Create()
           .WithPath("/api/users")
           .UsingGet())
       .RespondWith(WireMock.ResponseBuilders.Response.Create()
           .WithStatusCode(200)
           .WithBody("[{\"id\":1,\"name\":\"John\"}]"));
   ```

---

## Deserialization Failures

### Issue: "JSON deserialization failed"

**Symptoms:**
- `JsonException` when calling `ShouldReturn<T>()`
- "Unable to deserialize response body"

**Solutions:**

1. **Check Response Content**
   ```csharp
   try
   {
       var user = await api.For("/users/1")
           .Get()
           .ShouldReturn<User>();
   }
   catch (ApiAssertionException ex)
   {
       Console.WriteLine($"Response body: {ex.ResponseBody}");
       // Check if response is valid JSON
   }
   ```

2. **Validate JSON Format**
   ```csharp
   var result = await api.For("/users/1").Get();
   Console.WriteLine($"Raw response: {result.RawBody}");
   
   // Test JSON parsing manually
   try
   {
       var user = JsonSerializer.Deserialize<User>(result.RawBody);
   }
   catch (JsonException ex)
   {
       Console.WriteLine($"JSON error: {ex.Message}");
   }
   ```

3. **Handle Different Response Types**
   ```csharp
   // For error responses
   try
   {
       var user = await api.For("/users/1")
           .Get()
           .ShouldReturn<User>();
   }
   catch (ApiAssertionException ex)
   {
       // Check if it's an error response
       if (ex.ResponseBody.Contains("error"))
       {
           var error = JsonSerializer.Deserialize<ErrorResponse>(ex.ResponseBody);
           Console.WriteLine($"API Error: {error.Message}");
       }
   }
   ```

### Issue: "Property mapping failed"

**Symptoms:**
- Deserialization succeeds but properties are null/default
- Case sensitivity issues

**Solutions:**

1. **Use JsonPropertyName Attributes**
   ```csharp
   public class User
   {
       [JsonPropertyName("user_id")]
       public int Id { get; set; }
       
       [JsonPropertyName("full_name")]
       public string Name { get; set; } = string.Empty;
   }
   ```

2. **Configure JsonSerializer Options**
   ```csharp
   var options = new JsonSerializerOptions
   {
       PropertyNameCaseInsensitive = true,
       PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   };
   ```

3. **Handle Null Values**
   ```csharp
   public class User
   {
       public int Id { get; set; }
       public string Name { get; set; } = string.Empty; // Default value
       public string? Email { get; set; } // Nullable for optional fields
   }
   ```

---

## Timeout Issues

### Issue: "Request timed out"

**Symptoms:**
- `TaskCanceledException` or `TimeoutException`
- Requests hang indefinitely

**Solutions:**

1. **Set Appropriate Timeout**
   ```csharp
   var data = await api.For("/slow-endpoint")
       .WithTimeout(TimeSpan.FromMinutes(5))
       .Get()
       .ShouldReturn<Data>();
   ```

2. **Configure Global Timeout**
   ```csharp
   var defaults = new DefaultApiDefaults(
       timeout: TimeSpan.FromSeconds(30)
   );
   var api = new Api(executor, defaults);
   ```

3. **Handle Timeout Gracefully**
   ```csharp
   try
   {
       var data = await api.For("/endpoint")
           .WithTimeout(TimeSpan.FromSeconds(10))
           .Get()
           .ShouldReturn<Data>();
   }
   catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
   {
       Console.WriteLine("Request timed out - consider increasing timeout or optimizing endpoint");
   }
   ```

### Issue: "HttpClient timeout not working"

**Symptoms:**
- Timeout settings ignored
- Requests still hang

**Solutions:**

1. **Configure HttpClient Timeout**
   ```csharp
   var httpClient = new HttpClient
   {
       Timeout = TimeSpan.FromSeconds(30)
   };
   ```

2. **Use CancellationToken**
   ```csharp
   var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
   // Note: NaturalApi handles this internally
   ```

3. **Check Network Issues**
   ```csharp
   // Test connectivity first
   try
   {
       var response = await httpClient.GetAsync("https://api.example.com/health");
   }
   catch (HttpRequestException ex)
   {
       Console.WriteLine($"Network issue: {ex.Message}");
   }
   ```

---

## Base URL Resolution

### Issue: "Base URL not resolved correctly"

**Symptoms:**
- 404 errors for relative endpoints
- Incorrect URL construction

**Solutions:**

1. **Check Base URL Configuration**
   ```csharp
   var api = new Api("https://api.example.com");
   var result = await api.For("/users") // Results in https://api.example.com/users
       .Get()
       .ShouldReturn<List<User>>();
   ```

2. **Use Absolute URLs**
   ```csharp
   var result = await api.For("https://api.example.com/users")
       .Get()
       .ShouldReturn<List<User>>();
   ```

3. **Configure Defaults Provider**
   ```csharp
   var defaults = new DefaultApiDefaults(
       baseUri: new Uri("https://api.example.com")
   );
   var api = new Api(executor, defaults);
   ```

### Issue: "Path parameters not replaced"

**Symptoms:**
- URLs contain literal `{id}` instead of actual values
- 404 errors for parameterized endpoints

**Solutions:**

1. **Use WithPathParam Method**
   ```csharp
   var user = await api.For("/users/{id}")
       .WithPathParam("id", 123)
       .Get()
       .ShouldReturn<User>();
   ```

2. **Use WithPathParams for Multiple Parameters**
   ```csharp
   var post = await api.For("/users/{userId}/posts/{postId}")
       .WithPathParams(new { userId = 123, postId = 456 })
       .Get()
       .ShouldReturn<Post>();
   ```

3. **Check Parameter Names**
   ```csharp
   // Make sure parameter names match exactly
   .WithPathParam("userId", 123) // Matches {userId} in URL
   ```

---

## DI Registration Issues

### Issue: "Service not registered in DI container"

**Symptoms:**
- `InvalidOperationException` when resolving services
- Null reference exceptions

**Solutions:**

1. **Register All Required Services**
   ```csharp
   services.AddHttpClient();
   services.AddNaturalApi();
   services.AddSingleton<IApiAuthProvider, MyAuthProvider>();
   ```

2. **Use ServiceCollectionExtensions**
   ```csharp
   services.AddNaturalApiWithAuth<MyAuthProvider>(new MyAuthProvider());
   ```

3. **Check Service Lifetime**
   ```csharp
   // For scoped services
   services.AddScoped<IApi>(provider =>
   {
       var httpClient = provider.GetRequiredService<HttpClient>();
       return new Api(new HttpClientExecutor(httpClient));
   });
   ```

### Issue: "Circular dependency detected"

**Symptoms:**
- `InvalidOperationException: Circular dependency detected`

**Solutions:**

1. **Use Lazy Resolution**
   ```csharp
   public class MyService
   {
       private readonly Lazy<IApi> _api;
       
       public MyService(IServiceProvider serviceProvider)
       {
           _api = new Lazy<IApi>(() => serviceProvider.GetRequiredService<IApi>());
       }
   }
   ```

2. **Use Factory Pattern**
   ```csharp
   services.AddSingleton<IApiFactory, ApiFactory>();
   services.AddScoped<MyService>();
   ```

---

## Mock Setup Problems

### Issue: "Mock response not returned"

**Symptoms:**
- Tests fail with unexpected responses
- Mock setup not working

**Solutions:**

1. **Check Mock Setup**
   ```csharp
   [TestInitialize]
   public void Setup()
   {
       _mockExecutor = new MockHttpExecutor();
       _api = new Api(_mockExecutor);
   }

   [TestMethod]
   public async Task Test_Should_Use_Mock_Response()
   {
       // Arrange
       _mockExecutor.SetupResponse(200, """{"id":1,"name":"Test"}""");

       // Act
       var user = await _api.For("/users/1")
           .Get()
           .ShouldReturn<User>();

       // Assert
       Assert.AreEqual(1, user.Id);
       Assert.AreEqual("Test", user.Name);
   }
   ```

2. **Verify Request Specification**
   ```csharp
   var spec = _mockExecutor.LastSpec;
   Assert.AreEqual("/users/1", spec.Endpoint);
   Assert.AreEqual(HttpMethod.Get, spec.Method);
   ```

3. **Check Response Format**
   ```csharp
   _mockExecutor.SetupResponse(200, JsonSerializer.Serialize(new User { Id = 1, Name = "Test" }));
   ```

### Issue: "Mock not working in integration tests"

**Symptoms:**
- Integration tests make real HTTP calls instead of using mocks

**Solutions:**

1. **Use Test-Specific Configuration**
   ```csharp
   [TestClass]
   public class IntegrationTests
   {
       private IApi _api;

       [TestInitialize]
       public void Setup()
       {
           // Use real HTTP client for integration tests
           var httpClient = new HttpClient();
           var executor = new HttpClientExecutor(httpClient);
           _api = new Api(executor);
       }
   }
   ```

2. **Separate Unit and Integration Tests**
   ```csharp
   [TestMethod]
   [TestCategory("Unit")]
   public async Task Unit_Test_With_Mock() { }

   [TestMethod]
   [TestCategory("Integration")]
   public async Task Integration_Test_With_Real_API() { }
   ```

---

## Performance Issues

### Issue: "Slow API calls"

**Symptoms:**
- Requests take too long
- Timeout errors

**Solutions:**

1. **Optimize Request Configuration**
   ```csharp
   var data = await api.For("/data")
       .WithTimeout(TimeSpan.FromSeconds(30))
       .Get()
       .ShouldReturn<Data>();
   ```

2. **Use Connection Pooling**
   ```csharp
   services.AddHttpClient<IApi>(client =>
   {
       client.Timeout = TimeSpan.FromSeconds(30);
   });
   ```

3. **Implement Caching**
   ```csharp
   public class CachingApiService
   {
       private readonly IMemoryCache _cache;
       private readonly IApi _api;

       public async Task<T> GetDataAsync<T>(string endpoint, TimeSpan cacheDuration)
       {
           var cacheKey = $"api:{endpoint}";
           
           if (_cache.TryGetValue(cacheKey, out T cachedData))
           {
               return cachedData;
           }

           var data = await _api.For(endpoint)
               .Get()
               .ShouldReturn<T>();

           _cache.Set(cacheKey, data, cacheDuration);
           return data;
       }
   }
   ```

### Issue: "Memory leaks in long-running applications"

**Symptoms:**
- Memory usage increases over time
- OutOfMemoryException

**Solutions:**

1. **Dispose Resources Properly**
   ```csharp
   public class ApiService : IDisposable
   {
       private readonly HttpClient _httpClient;
       private readonly IApi _api;

       public ApiService()
       {
           _httpClient = new HttpClient();
           _api = new Api(new HttpClientExecutor(_httpClient));
       }

       public void Dispose()
       {
           _httpClient?.Dispose();
       }
   }
   ```

2. **Use HttpClientFactory**
   ```csharp
   services.AddHttpClient<IApi>();
   // HttpClientFactory manages lifecycle automatically
   ```

---

## Debugging Techniques

### Enable Detailed Logging

```csharp
public class LoggingApiService
{
    private readonly IApi _api;
    private readonly ILogger<LoggingApiService> _logger;

    public async Task<T> GetDataAsync<T>(string endpoint)
    {
        _logger.LogInformation("Making request to {Endpoint}", endpoint);
        
        try
        {
            var result = await _api.For(endpoint)
                .Get()
                .ShouldReturn<T>();
            
            _logger.LogInformation("Request successful");
            return result;
        }
        catch (ApiExecutionException ex)
        {
            _logger.LogError(ex, "Request failed: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (ApiAssertionException ex)
        {
            _logger.LogError(ex, "Validation failed: {Message}", ex.Message);
            throw;
        }
    }
}
```

### Inspect Request/Response Details

```csharp
public class DebugApiService
{
    private readonly IApi _api;

    public async Task<T> GetDataWithDebugInfo<T>(string endpoint)
    {
        try
        {
            var result = await _api.For(endpoint)
                .Get()
                .ShouldReturn<T>();
            
            return result;
        }
        catch (ApiExecutionException ex)
        {
            Console.WriteLine($"Request URL: {ex.RequestUrl}");
            Console.WriteLine($"HTTP Method: {ex.HttpMethod}");
            Console.WriteLine($"Status Code: {ex.StatusCode}");
            Console.WriteLine($"Response Body: {ex.ResponseBody}");
            Console.WriteLine($"Headers: {string.Join(", ", ex.Headers)}");
            throw;
        }
    }
}
```

### Use Network Monitoring

```csharp
public class NetworkMonitoringApiService
{
    private readonly IApi _api;
    private readonly ILogger<NetworkMonitoringApiService> _logger;

    public async Task<T> GetDataWithMonitoring<T>(string endpoint)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _api.For(endpoint)
                .Get()
                .ShouldReturn<T>();
            
            stopwatch.Stop();
            _logger.LogInformation("Request completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic setup and first steps
- **[Error Handling](error-handling.md)** - Comprehensive error handling patterns
- **[Testing Guide](testing-guide.md)** - Testing strategies and debugging
- **[Configuration](configuration.md)** - Proper configuration setup
- **[Authentication](authentication.md)** - Authentication troubleshooting
- **[Examples](examples.md)** - Working examples and patterns
- **[Request Building](request-building.md)** - Request configuration issues
- **[HTTP Verbs](http-verbs.md)** - HTTP method issues
- **[Assertions](assertions.md)** - Validation problems
- **[API Reference](api-reference.md)** - Complete interface documentation
- **[Dependency Injection Guide](di.md)** - DI setup and registration
