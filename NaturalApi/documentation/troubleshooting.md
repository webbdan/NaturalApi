# Troubleshooting Guide

> Common issues, solutions, and debugging techniques for NaturalApi.

---

## Table of Contents

- [Common Setup Issues](#common-setup-issues)
- [Authentication Problems](#authentication-problems)
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
