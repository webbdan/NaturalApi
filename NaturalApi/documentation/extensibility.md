# Extensibility Guide

> NaturalApi is designed to be highly extensible. This guide covers creating custom executors, validators, auth providers, and extending the fluent DSL with new capabilities.

---

## Table of Contents

- [Custom HTTP Executors](#custom-http-executors)
- [Custom Authentication Providers](#custom-authentication-providers)
- [Custom Validators](#custom-validators)
- [Custom Defaults Providers](#custom-defaults-providers)
- [Extending the Fluent DSL](#extending-the-fluent-dsl)
- [Custom Result Contexts](#custom-result-contexts)
- [Advanced Extensibility Patterns](#advanced-extensibility-patterns)

---

## Custom HTTP Executors

### Generic Executor Registration

NaturalApi now supports generic registration of custom HTTP executors, making it easy to use alternative HTTP clients like RestSharp, Playwright, or any other library.

#### Simple Generic Registration

```csharp
// Register a custom executor with default constructor
services.AddNaturalApi<MyCustomHttpExecutor>();
```

#### Factory-Based Registration

```csharp
// Register with a factory function
services.AddNaturalApi<MyCustomHttpExecutor>(provider => 
    new MyCustomHttpExecutor("https://api.example.com"));
```

#### Options-Based Registration

```csharp
// Register with configuration options
services.AddNaturalApi<MyCustomHttpExecutor, MyExecutorOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.CustomSetting = "value";
});
```

#### Complete RestSharp Example

Here's a complete example of integrating RestSharp as a custom executor:

```csharp
// 1. Create RestSharp executor
public class RestSharpHttpExecutor : IHttpExecutor
{
    private readonly RestClient _restClient;

    public RestSharpHttpExecutor(RestClient restClient)
    {
        _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        try
        {
            var url = BuildUrl(spec);
            var request = new RestRequest(url, GetRestSharpMethod(spec.Method));
            
            // Add headers, query params, body, etc.
            foreach (var header in spec.Headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
            
            if (spec.Body != null && IsBodyMethod(spec.Method))
            {
                request.AddJsonBody(spec.Body);
            }
            
            var response = _restClient.Execute(request);
            return new RestSharpApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            throw new ApiExecutionException("RestSharp request failed", ex, spec);
        }
    }
    
    // ... implementation details
}

// 2. Register with dependency injection
services.AddNaturalApi<RestSharpHttpExecutor>(provider => 
    new RestSharpHttpExecutor(new RestClient("https://api.example.com")));

// 3. Use exactly like HttpClient-based NaturalApi
var api = serviceProvider.GetRequiredService<IApi>();
var result = await api.For("/users").Get().ShouldBeSuccessful();
```

### Basic Custom Executor

```csharp
public class CustomHttpExecutor : IHttpExecutor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public CustomHttpExecutor(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        _logger.LogInformation($"Executing {spec.Method} request to {spec.Endpoint}");
        
        try
        {
            // Custom request building logic
            var request = BuildRequest(spec);
            var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            
            return new ApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed: {Message}", ex.Message);
            throw new ApiExecutionException("Request execution failed", ex);
        }
    }

    private HttpRequestMessage BuildRequest(ApiRequestSpec spec)
    {
        var request = new HttpRequestMessage(spec.Method, spec.Endpoint);
        
        // Add headers
        foreach (var header in spec.Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
        
        // Add body if present
        if (spec.Body != null)
        {
            var json = JsonSerializer.Serialize(spec.Body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        return request;
    }
}
```

### Authenticated Executor

```csharp
public class CustomAuthenticatedExecutor : IAuthenticatedHttpExecutor
{
    private readonly HttpClient _httpClient;
    private readonly IApiAuthProvider _authProvider;

    public CustomAuthenticatedExecutor(HttpClient httpClient, IApiAuthProvider authProvider)
    {
        _httpClient = httpClient;
        _authProvider = authProvider;
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        return ExecuteAsync(spec, _authProvider, null, false).GetAwaiter().GetResult();
    }

    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, bool suppressAuth)
    {
        var request = await BuildAuthenticatedRequest(spec, authProvider, username, suppressAuth);
        var response = await _httpClient.SendAsync(request);
        
        return new ApiResultContext(response, this);
    }

    private async Task<HttpRequestMessage> BuildAuthenticatedRequest(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, bool suppressAuth)
    {
        var request = new HttpRequestMessage(spec.Method, spec.Endpoint);
        
        // Add authentication if not suppressed
        if (!suppressAuth && authProvider != null)
        {
            var token = await authProvider.GetAuthTokenAsync(username);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        
        // Add other headers
        foreach (var header in spec.Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
        
        return request;
    }
}
```

### Retry Executor

```csharp
public class RetryHttpExecutor : IHttpExecutor
{
    private readonly IHttpExecutor _inner;
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;

    public RetryHttpExecutor(IHttpExecutor inner, int maxRetries = 3, TimeSpan? delay = null)
    {
        _inner = inner;
        _maxRetries = maxRetries;
        _delay = delay ?? TimeSpan.FromSeconds(1);
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return _inner.Execute(spec);
            }
            catch (ApiExecutionException ex) when (IsRetryableError(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                Thread.Sleep(_delay);
            }
        }
        
        throw lastException ?? new ApiExecutionException("All retry attempts failed");
    }

    private bool IsRetryableError(ApiExecutionException ex)
    {
        return ex.InnerException is HttpRequestException ||
               ex.InnerException is TaskCanceledException ||
               (ex.StatusCode >= 500 && ex.StatusCode < 600);
    }
}
```

### Circuit Breaker Executor

```csharp
public class CircuitBreakerExecutor : IHttpExecutor
{
    private readonly IHttpExecutor _inner;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;

    public CircuitBreakerExecutor(IHttpExecutor inner, int failureThreshold = 5, TimeSpan? timeout = null)
    {
        _inner = inner;
        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _timeout)
            {
                _state = CircuitBreakerState.HalfOpen;
            }
            else
            {
                throw new ApiExecutionException("Circuit breaker is open");
            }
        }

        try
        {
            var result = _inner.Execute(spec);
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
            return result;
        }
        catch (ApiExecutionException ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
            
            throw;
        }
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
```

---

## Custom Authentication Providers

### OAuth 2.0 Provider

```csharp
public class OAuth2AuthProvider : IApiAuthProvider
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenEndpoint;
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private DateTime _expiresAt;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public OAuth2AuthProvider(string clientId, string clientSecret, string tokenEndpoint, HttpClient httpClient)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
        _httpClient = httpClient;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_accessToken == null || DateTime.UtcNow >= _expiresAt)
            {
                await RefreshTokenAsync();
            }
            return _accessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshTokenAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            })
        };

        var response = await _httpClient.SendAsync(request);
        var tokenResponse = await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>();
        
        _accessToken = tokenResponse.AccessToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
    }
}

public class OAuth2TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}
```

### JWT Token Provider

```csharp
public class JwtAuthProvider : IApiAuthProvider
{
    private readonly IJwtTokenService _jwtService;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtAuthProvider(IJwtTokenService jwtService, string issuer, string audience)
    {
        _jwtService = jwtService;
        _issuer = issuer;
        _audience = audience;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = username ?? "system",
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        return await _jwtService.GenerateTokenAsync(claims);
    }
}
```

### Multi-Tenant Auth Provider

```csharp
public class MultiTenantAuthProvider : IApiAuthProvider
{
    private readonly Dictionary<string, IApiAuthProvider> _tenantProviders;
    private readonly ITenantResolver _tenantResolver;

    public MultiTenantAuthProvider(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
        _tenantProviders = new Dictionary<string, IApiAuthProvider>();
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var tenant = await _tenantResolver.GetCurrentTenantAsync();
        
        if (!_tenantProviders.TryGetValue(tenant.Id, out var provider))
        {
            provider = CreateTenantProvider(tenant);
            _tenantProviders[tenant.Id] = provider;
        }

        return await provider.GetAuthTokenAsync(username);
    }

    private IApiAuthProvider CreateTenantProvider(Tenant tenant)
    {
        return new OAuth2AuthProvider(
            tenant.ClientId,
            tenant.ClientSecret,
            tenant.TokenEndpoint,
            new HttpClient()
        );
    }
}
```

---

## Custom Validators

### Custom API Validator

```csharp
public class CustomApiValidator : IApiValidator
{
    private readonly ILogger _logger;

    public CustomApiValidator(ILogger logger)
    {
        _logger = logger;
    }

    public void ValidateStatus(HttpResponseMessage response, int expected)
    {
        if ((int)response.StatusCode != expected)
        {
            var message = $"Expected status {expected} but got {(int)response.StatusCode} for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}";
            _logger.LogWarning("Status validation failed: {Message}", message);
            throw new ApiAssertionException(message, response);
        }
    }

    public void ValidateHeaders(HttpResponseMessage response, Func<IDictionary<string, string>, bool> predicate)
    {
        var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
        
        if (!predicate(headers))
        {
            var message = $"Header validation failed for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}";
            _logger.LogWarning("Header validation failed: {Message}", message);
            throw new ApiAssertionException(message, response);
        }
    }

    public void ValidateBody<T>(string rawBody, Action<T> validator)
    {
        try
        {
            var body = JsonSerializer.Deserialize<T>(rawBody);
            if (body == null)
            {
                throw new ApiAssertionException("Response body is null");
            }
            
            validator(body);
        }
        catch (JsonException ex)
        {
            throw new ApiAssertionException($"Failed to deserialize response body: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new ApiAssertionException($"Body validation failed: {ex.Message}");
        }
    }
}
```

### Business Rule Validator

```csharp
public class BusinessRuleValidator : IApiValidator
{
    public void ValidateStatus(HttpResponseMessage response, int expected)
    {
        // Standard status validation
        if ((int)response.StatusCode != expected)
        {
            throw new ApiAssertionException($"Expected status {expected} but got {(int)response.StatusCode}");
        }
    }

    public void ValidateHeaders(HttpResponseMessage response, Func<IDictionary<string, string>, bool> predicate)
    {
        var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
        
        if (!predicate(headers))
        {
            throw new ApiAssertionException("Header validation failed");
        }
    }

    public void ValidateBody<T>(string rawBody, Action<T> validator)
    {
        var body = JsonSerializer.Deserialize<T>(rawBody);
        if (body == null)
        {
            throw new ApiAssertionException("Response body is null");
        }
        
        try
        {
            validator(body);
        }
        catch (Exception ex)
        {
            throw new ApiAssertionException($"Business rule validation failed: {ex.Message}");
        }
    }
}
```

---

## Custom Defaults Providers

### Environment-Based Defaults

```csharp
public class EnvironmentDefaultsProvider : IApiDefaultsProvider
{
    private readonly IConfiguration _configuration;

    public EnvironmentDefaultsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Uri? BaseUri => new Uri(_configuration["ApiSettings:BaseUrl"]);

    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["User-Agent"] = _configuration["ApiSettings:UserAgent"],
        ["X-API-Key"] = _configuration["ApiSettings:ApiKey"]
    };

    public TimeSpan Timeout => TimeSpan.FromSeconds(_configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 30));

    public IApiAuthProvider? AuthProvider => null;
}
```

### Multi-Environment Defaults

```csharp
public class MultiEnvironmentDefaultsProvider : IApiDefaultsProvider
{
    private readonly string _environment;
    private readonly IConfiguration _configuration;

    public MultiEnvironmentDefaultsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    public Uri? BaseUri => _environment switch
    {
        "Development" => new Uri("https://dev-api.example.com"),
        "Staging" => new Uri("https://staging-api.example.com"),
        "Production" => new Uri("https://api.example.com"),
        _ => new Uri("https://localhost:5001")
    };

    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["X-Environment"] = _environment,
        ["X-Version"] = _configuration["ApiSettings:Version"]
    };

    public TimeSpan Timeout => _environment == "Production" 
        ? TimeSpan.FromSeconds(30) 
        : TimeSpan.FromSeconds(60);

    public IApiAuthProvider? AuthProvider => null;
}
```

---

## Extending the Fluent DSL

### Custom Extension Methods

```csharp
public static class ApiContextExtensions
{
    public static IApiContext WithApiKey(this IApiContext context, string apiKey)
    {
        return context.WithHeader("X-API-Key", apiKey);
    }

    public static IApiContext WithVersion(this IApiContext context, string version)
    {
        return context.WithHeader("X-API-Version", version);
    }

    public static IApiContext WithCorrelationId(this IApiContext context, string correlationId)
    {
        return context.WithHeader("X-Correlation-ID", correlationId);
    }

    public static IApiContext WithRequestId(this IApiContext context)
    {
        return context.WithHeader("X-Request-ID", Guid.NewGuid().ToString());
    }
}

// Usage
var data = await api.For("/data")
    .WithApiKey("your-api-key")
    .WithVersion("v1")
    .WithCorrelationId("correlation-123")
    .WithRequestId()
    .Get()
    .ShouldReturn<Data>();
```

### Logging Extensions

```csharp
public static class LoggingApiContextExtensions
{
    public static IApiContext LogTo(this IApiContext context, ILogger logger)
    {
        return new LoggingApiContext(context, logger);
    }
}

public class LoggingApiContext : IApiContext
{
    private readonly IApiContext _inner;
    private readonly ILogger _logger;

    public LoggingApiContext(IApiContext inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public IApiContext WithHeader(string key, string value)
    {
        _logger.LogDebug("Adding header: {Key} = {Value}", key, value);
        return new LoggingApiContext(_inner.WithHeader(key, value), _logger);
    }

    public IApiContext WithHeaders(IDictionary<string, string> headers)
    {
        _logger.LogDebug("Adding {Count} headers", headers.Count);
        return new LoggingApiContext(_inner.WithHeaders(headers), _logger);
    }

    // Implement other methods similarly...
    public IApiResultContext Get()
    {
        _logger.LogInformation("Making GET request");
        return _inner.Get();
    }

    public IApiResultContext Post(object? body = null)
    {
        _logger.LogInformation("Making POST request with body: {Body}", body);
        return _inner.Post(body);
    }

    // ... other HTTP methods
}
```

### Caching Extensions

```csharp
public static class CachingApiContextExtensions
{
    public static IApiContext WithCache(this IApiContext context, TimeSpan duration)
    {
        return context.WithHeader("Cache-Control", $"max-age={duration.TotalSeconds}");
    }

    public static IApiContext NoCache(this IApiContext context)
    {
        return context.WithHeader("Cache-Control", "no-cache");
    }
}
```

---

## Custom Result Contexts

### Enhanced Result Context

```csharp
public class EnhancedApiResultContext : IApiResultContext
{
    private readonly IApiResultContext _inner;
    private readonly ILogger _logger;

    public EnhancedApiResultContext(IApiResultContext inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public HttpResponseMessage Response => _inner.Response;
    public int StatusCode => _inner.StatusCode;
    public IDictionary<string, string> Headers => _inner.Headers;
    public string RawBody => _inner.RawBody;

    public T BodyAs<T>()
    {
        try
        {
            return _inner.BodyAs<T>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response body to {Type}", typeof(T).Name);
            throw;
        }
    }

    public IApiResultContext ShouldReturn<T>(int? status = null, Action<T>? bodyValidator = null, Func<IDictionary<string, string>, bool>? headers = null)
    {
        _logger.LogDebug("Validating response with status: {Status}", status);
        return _inner.ShouldReturn(status, bodyValidator, headers);
    }

    public IApiResultContext Then(Action<IApiResultContext> next)
    {
        return _inner.Then(next);
    }
}
```

---

## Advanced Extensibility Patterns

### Decorator Pattern

```csharp
public class LoggingHttpExecutor : IHttpExecutor
{
    private readonly IHttpExecutor _inner;
    private readonly ILogger _logger;

    public LoggingHttpExecutor(IHttpExecutor inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        _logger.LogInformation("Executing {Method} {Endpoint}", spec.Method, spec.Endpoint);
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = _inner.Execute(spec);
            stopwatch.Stop();
            
            _logger.LogInformation("Request completed in {ElapsedMs}ms with status {StatusCode}", 
                stopwatch.ElapsedMilliseconds, result.StatusCode);
            
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

// Usage with decorator chain
var executor = new LoggingHttpExecutor(
    new RetryHttpExecutor(
        new CircuitBreakerExecutor(
            new HttpClientExecutor(httpClient)
        )
    ),
    logger
);
```

### Factory Pattern

```csharp
public interface IApiFactory
{
    IApi CreateApi(string baseUrl);
    IApi CreateApi(string baseUrl, IApiAuthProvider authProvider);
    IApi CreateApi(string baseUrl, IApiDefaultsProvider defaults);
}

public class ApiFactory : IApiFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiFactory> _logger;

    public ApiFactory(IHttpClientFactory httpClientFactory, ILogger<ApiFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IApi CreateApi(string baseUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var executor = new HttpClientExecutor(httpClient);
        return new Api(executor);
    }

    public IApi CreateApi(string baseUrl, IApiAuthProvider authProvider)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var executor = new AuthenticatedHttpClientExecutor(httpClient);
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        return new Api(executor, defaults);
    }

    public IApi CreateApi(string baseUrl, IApiDefaultsProvider defaults)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var executor = new HttpClientExecutor(httpClient);
        return new Api(executor, defaults);
    }
}
```

### Strategy Pattern

```csharp
public interface IRequestStrategy
{
    Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec);
}

public class StandardRequestStrategy : IRequestStrategy
{
    private readonly IHttpExecutor _executor;

    public StandardRequestStrategy(IHttpExecutor executor)
    {
        _executor = executor;
    }

    public Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec)
    {
        return Task.FromResult(_executor.Execute(spec));
    }
}

public class RetryRequestStrategy : IRequestStrategy
{
    private readonly IHttpExecutor _executor;
    private readonly int _maxRetries;

    public RetryRequestStrategy(IHttpExecutor executor, int maxRetries = 3)
    {
        _executor = executor;
        _maxRetries = maxRetries;
    }

    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return _executor.Execute(spec);
            }
            catch (ApiExecutionException ex) when (attempt < _maxRetries)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
        
        throw lastException ?? new ApiExecutionException("All retry attempts failed");
    }
}
```

---

## Best Practices

### 1. Follow Single Responsibility Principle

```csharp
// Good - focused on one concern
public class LoggingHttpExecutor : IHttpExecutor
{
    // Only handles logging
}

public class RetryHttpExecutor : IHttpExecutor
{
    // Only handles retries
}

// Avoid - multiple concerns
public class LoggingRetryCircuitBreakerExecutor : IHttpExecutor
{
    // Too many responsibilities
}
```

### 2. Use Dependency Injection

```csharp
// Register custom components
services.AddSingleton<IHttpExecutor, CustomHttpExecutor>();
services.AddSingleton<IApiAuthProvider, CustomAuthProvider>();
services.AddSingleton<IApiValidator, CustomValidator>();
```

### 3. Provide Configuration Options

```csharp
public class ConfigurableHttpExecutor : IHttpExecutor
{
    private readonly IHttpExecutor _inner;
    private readonly ExecutorOptions _options;

    public ConfigurableHttpExecutor(IHttpExecutor inner, ExecutorOptions options)
    {
        _inner = inner;
        _options = options;
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        if (_options.EnableLogging)
        {
            // Add logging
        }
        
        if (_options.EnableRetry)
        {
            // Add retry logic
        }
        
        return _inner.Execute(spec);
    }
}

public class ExecutorOptions
{
    public bool EnableLogging { get; set; } = true;
    public bool EnableRetry { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
}
```

### 4. Test Your Extensions

```csharp
[TestClass]
public class CustomExecutorTests
{
    [TestMethod]
    public void CustomExecutor_Should_Add_Custom_Headers()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var customExecutor = new CustomHttpExecutor(mockExecutor);
        var api = new Api(customExecutor);

        // Act
        api.For("/test").Get().ShouldReturn<object>();

        // Assert
        var spec = mockExecutor.LastSpec;
        Assert.IsTrue(spec.Headers.ContainsKey("X-Custom-Header"));
    }
}
```

---

## Generic Executor Registration

NaturalApi now supports generic registration of custom HTTP executors, making it easy to use alternative HTTP clients like RestSharp, Playwright, or any other library.

### Simple Generic Registration

```csharp
// Register a custom executor with default constructor
services.AddNaturalApi<MyCustomHttpExecutor>();
```

### Factory-Based Registration

```csharp
// Register with a factory function
services.AddNaturalApi<MyCustomHttpExecutor>(provider => 
    new MyCustomHttpExecutor("https://api.example.com"));
```

### Options-Based Registration

```csharp
// Register with configuration options
services.AddNaturalApi<MyCustomHttpExecutor, MyExecutorOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.CustomSetting = "value";
});
```

### Complete RestSharp Example

Here's a complete example of integrating RestSharp as a custom executor:

```csharp
// 1. Create RestSharp executor
public class RestSharpHttpExecutor : IHttpExecutor
{
    private readonly RestClient _restClient;

    public RestSharpHttpExecutor(RestClient restClient)
    {
        _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        try
        {
            var url = BuildUrl(spec);
            var request = new RestRequest(url, GetRestSharpMethod(spec.Method));
            
            // Add headers, query params, body, etc.
            foreach (var header in spec.Headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
            
            if (spec.Body != null && IsBodyMethod(spec.Method))
            {
                request.AddJsonBody(spec.Body);
            }
            
            var response = _restClient.Execute(request);
            return new RestSharpApiResultContext(response, this);
        }
        catch (Exception ex)
        {
            throw new ApiExecutionException("RestSharp request failed", ex, spec);
        }
    }
    
    // ... implementation details
}

// 2. Register with dependency injection
services.AddNaturalApi<RestSharpHttpExecutor>(provider => 
    new RestSharpHttpExecutor(new RestClient("https://api.example.com")));

// 3. Use exactly like HttpClient-based NaturalApi
var api = serviceProvider.GetRequiredService<IApi>();
var result = await api.For("/users").Get().ShouldBeSuccessful();
```

### Best Practices for Custom Executors

#### 1. Handle All Request Components
Your custom executor should handle all aspects of the `ApiRequestSpec`:

```csharp
public IApiResultContext Execute(ApiRequestSpec spec)
{
    // Handle URL building
    var url = BuildUrl(spec);
    
    // Handle HTTP method
    var request = new RestRequest(url, GetMethod(spec.Method));
    
    // Handle headers
    foreach (var header in spec.Headers)
    {
        request.AddHeader(header.Key, header.Value);
    }
    
    // Handle query parameters
    foreach (var param in spec.QueryParams)
    {
        request.AddQueryParameter(param.Key, param.Value?.ToString());
    }
    
    // Handle path parameters
    foreach (var param in spec.PathParams)
    {
        url = url.Replace($"{{{param.Key}}}", param.Value?.ToString());
    }
    
    // Handle request body
    if (spec.Body != null && IsBodyMethod(spec.Method))
    {
        request.AddJsonBody(spec.Body);
    }
    
    // Handle cookies
    if (spec.Cookies != null)
    {
        foreach (var cookie in spec.Cookies)
        {
            request.AddCookie(cookie.Key, cookie.Value);
        }
    }
    
    // Handle timeout
    if (spec.Timeout.HasValue)
    {
        request.Timeout = (int)spec.Timeout.Value.TotalMilliseconds;
    }
    
    // Execute and return result
    var response = _client.Execute(request);
    return new MyApiResultContext(response, this);
}
```

#### 2. Implement Proper Result Context
Your result context should implement all `IApiResultContext` methods:

```csharp
public class MyApiResultContext : IApiResultContext
{
    private readonly MyResponse _response;
    private readonly MyHttpExecutor _executor;

    public MyApiResultContext(MyResponse response, MyHttpExecutor executor)
    {
        _response = response;
        _executor = executor;
    }

    public int StatusCode => _response.StatusCode;
    public IDictionary<string, string> Headers => _response.Headers;
    public string RawBody => _response.Content;

    public T BodyAs<T>()
    {
        return JsonSerializer.Deserialize<T>(RawBody) 
            ?? throw new InvalidOperationException("Deserialization failed");
    }

    // Implement all validation methods...
    public IApiResultContext ShouldReturn<T>(int? status = null, Func<T, bool>? bodyValidator = null, Func<IDictionary<string, string>, bool>? headers = null)
    {
        // Validation logic
        return this;
    }
    
    // ... other methods
}
```

#### 3. Handle Async Executors
If your HTTP client is async, you can still implement the sync interface:

```csharp
public IApiResultContext Execute(ApiRequestSpec spec)
{
    // Use GetAwaiter().GetResult() for sync interface
    return ExecuteAsync(spec).GetAwaiter().GetResult();
}

private async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec)
{
    var response = await _asyncClient.SendAsync(request);
    return new MyApiResultContext(response, this);
}
```

#### 4. Configuration Options Pattern
Use strongly-typed options for configuration:

```csharp
public class MyExecutorOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public bool IgnoreSslErrors { get; set; } = false;
}

// Register with options
services.AddNaturalApi<MyHttpExecutor, MyExecutorOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromMinutes(2);
    options.DefaultHeaders["User-Agent"] = "MyApp/1.0";
});
```

#### 5. Error Handling
Always wrap exceptions with `ApiExecutionException`:

```csharp
try
{
    var response = _client.Execute(request);
    return new MyApiResultContext(response, this);
}
catch (Exception ex)
{
    throw new ApiExecutionException("Request execution failed", ex, spec);
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic concepts before extending
- **[Configuration](configuration.md)** - Setting up custom components
- **[Authentication](authentication.md)** - Custom authentication patterns
- **[Testing Guide](testing-guide.md)** - Testing custom extensions
- **[API Reference](api-reference.md)** - Complete interface documentation
- **[Examples](examples.md)** - Real-world extensibility scenarios
- **[Dependency Injection Guide](di.md)** - DI patterns for custom components
- **[Architecture Overview](architectureanddesign.md)** - Internal design for extension points
- **[Contributing](contributing.md)** - Contributing guidelines
