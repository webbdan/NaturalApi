# Error Handling Guide

> NaturalApi provides comprehensive error handling with readable exception messages and detailed debugging information. This guide covers exception types, error scenarios, and debugging techniques.

---

## Table of Contents

- [Exception Types](#exception-types)
- [Assertion Errors](#assertion-errors)
- [Execution Errors](#execution-errors)
- [Timeout Handling](#timeout-handling)
- [Network Errors](#network-errors)
- [Debugging Techniques](#debugging-techniques)
- [Error Recovery](#error-recovery)
- [Best Practices](#best-practices)

---

## Exception Types

NaturalApi throws two main exception types:

### ApiAssertionException

Thrown when response validation fails (status codes, body validation, headers).

```csharp
try
{
    var user = await api.For("/users/999")
        .Get()
        .ShouldReturn<User>(status: 200);
}
catch (ApiAssertionException ex)
{
    // Status code mismatch or validation failure
    Console.WriteLine($"Assertion failed: {ex.Message}");
}
```

### ApiExecutionException

Thrown when HTTP execution fails (network errors, timeouts, server errors).

```csharp
try
{
    var data = await api.For("/unreachable-endpoint")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    // Network error, timeout, or HTTP error
    Console.WriteLine($"Request failed: {ex.Message}");
}
```

---

## Assertion Errors

### Status Code Mismatches

```csharp
try
{
    await api.For("/users/999")
        .Get()
        .ShouldReturn(status: 200);
}
catch (ApiAssertionException ex)
{
    // Message: "Expected status 200 but got 404 for GET /users/999"
    Console.WriteLine($"Expected status 200, got {ex.ActualStatusCode}");
    Console.WriteLine($"Response body: {ex.ResponseBody}");
}
```

### Body Validation Failures

```csharp
try
{
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>(body: u => u.Name == "Expected Name");
}
catch (ApiAssertionException ex)
{
    // Message includes the failed assertion details
    Console.WriteLine($"Validation failed: {ex.Message}");
    Console.WriteLine($"Actual response: {ex.ResponseBody}");
}
```

### Header Validation Failures

```csharp
try
{
    var response = await api.For("/data")
        .Get()
        .ShouldReturn<Data>(headers: h => h.ContainsKey("X-Custom-Header"));
}
catch (ApiAssertionException ex)
{
    // Message: "Header validation failed for GET /data"
    Console.WriteLine($"Header validation failed: {ex.Message}");
    Console.WriteLine($"Available headers: {string.Join(", ", ex.Headers.Keys)}");
}
```

### Complex Validation Failures

```csharp
try
{
    var order = await api.For("/orders/123")
        .Get()
        .ShouldReturn<Order>(
            status: 200,
            body: o => o.Status == OrderStatus.Confirmed && o.Total > 0,
            headers: h => h.ContainsKey("X-Order-ID")
        );
}
catch (ApiAssertionException ex)
{
    // Detailed error message with all failed validations
    Console.WriteLine($"Multiple validations failed: {ex.Message}");
    Console.WriteLine($"Status: {ex.ActualStatusCode}");
    Console.WriteLine($"Headers: {string.Join(", ", ex.Headers.Keys)}");
    Console.WriteLine($"Body: {ex.ResponseBody}");
}
```

---

## Execution Errors

### Network Connectivity Issues

```csharp
try
{
    var data = await api.For("https://unreachable-server.com/api/data")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    // Network connectivity issues
    if (ex.InnerException is HttpRequestException httpEx)
    {
        Console.WriteLine($"Network error: {httpEx.Message}");
    }
    else if (ex.InnerException is TaskCanceledException)
    {
        Console.WriteLine("Request was cancelled (possibly due to timeout)");
    }
}
```

### HTTP Error Status Codes

```csharp
try
{
    var data = await api.For("/server-error")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    // Server returned 5xx status code
    Console.WriteLine($"Server error: {ex.Message}");
    Console.WriteLine($"Status code: {ex.StatusCode}");
    Console.WriteLine($"Response body: {ex.ResponseBody}");
}
```

### Authentication Failures

```csharp
try
{
    var data = await api.For("/protected")
        .UsingAuth("invalid-token")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    if (ex.StatusCode == 401)
    {
        Console.WriteLine("Authentication failed - invalid token");
    }
    else if (ex.StatusCode == 403)
    {
        Console.WriteLine("Access forbidden - insufficient permissions");
    }
}
```

---

## Timeout Handling

### Request Timeouts

```csharp
try
{
    var data = await api.For("/slow-endpoint")
        .WithTimeout(TimeSpan.FromSeconds(1))
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
{
    Console.WriteLine("Request timed out after 1 second");
}
```

### Global Timeout Configuration

```csharp
// Configure global timeout
var defaults = new DefaultApiDefaults(timeout: TimeSpan.FromSeconds(30));
var api = new Api(executor, defaults);

try
{
    var data = await api.For("/slow-endpoint")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
{
    Console.WriteLine("Request timed out after 30 seconds");
}
```

### Timeout with Retry Logic

```csharp
public async Task<Data> GetDataWithRetry(int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await api.For("/data")
                .WithTimeout(TimeSpan.FromSeconds(5))
                .Get()
                .ShouldReturn<Data>();
        }
        catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
        {
            if (attempt == maxRetries)
                throw;
            
            Console.WriteLine($"Attempt {attempt} timed out, retrying...");
            await Task.Delay(1000); // Wait 1 second before retry
        }
    }
    
    throw new InvalidOperationException("All retry attempts failed");
}
```

---

## Network Errors

### Connection Refused

```csharp
try
{
    var data = await api.For("http://localhost:9999/api/data")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    if (ex.InnerException is HttpRequestException httpEx)
    {
        Console.WriteLine($"Connection refused: {httpEx.Message}");
    }
}
```

### DNS Resolution Failures

```csharp
try
{
    var data = await api.For("https://nonexistent-domain.com/api/data")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    if (ex.InnerException is HttpRequestException httpEx)
    {
        Console.WriteLine($"DNS resolution failed: {httpEx.Message}");
    }
}
```

### SSL/TLS Errors

```csharp
try
{
    var data = await api.For("https://self-signed-cert.com/api/data")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    if (ex.InnerException is HttpRequestException httpEx)
    {
        Console.WriteLine($"SSL/TLS error: {httpEx.Message}");
    }
}
```

---

## Debugging Techniques

### Enable Detailed Logging

```csharp
// Configure HttpClient with logging
var httpClient = new HttpClient();
var executor = new HttpClientExecutor(httpClient);
var api = new Api(executor);

try
{
    var data = await api.For("/endpoint")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    // Log detailed information
    Console.WriteLine($"Request failed:");
    Console.WriteLine($"  URL: {ex.RequestUrl}");
    Console.WriteLine($"  Method: {ex.HttpMethod}");
    Console.WriteLine($"  Status: {ex.StatusCode}");
    Console.WriteLine($"  Headers: {string.Join(", ", ex.Headers)}");
    Console.WriteLine($"  Body: {ex.ResponseBody}");
    Console.WriteLine($"  Inner Exception: {ex.InnerException?.Message}");
}
```

### Response Inspection

```csharp
try
{
    var result = await api.For("/endpoint")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiAssertionException ex)
{
    // Inspect the actual response
    Console.WriteLine($"Expected status: {ex.ExpectedStatusCode}");
    Console.WriteLine($"Actual status: {ex.ActualStatusCode}");
    Console.WriteLine($"Response headers: {string.Join(", ", ex.Headers)}");
    Console.WriteLine($"Response body: {ex.ResponseBody}");
    
    // Check if it's a deserialization issue
    try
    {
        var actualData = JsonSerializer.Deserialize<Data>(ex.ResponseBody);
        Console.WriteLine("Response can be deserialized, validation logic issue");
    }
    catch (JsonException)
    {
        Console.WriteLine("Response cannot be deserialized to expected type");
    }
}
```

### Request/Response Logging

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
        _logger.LogInformation($"Making {spec.Method} request to {spec.Endpoint}");
        _logger.LogInformation($"Headers: {string.Join(", ", spec.Headers)}");
        
        try
        {
            var result = _inner.Execute(spec);
            _logger.LogInformation($"Response: {result.StatusCode}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Request failed: {ex.Message}");
            throw;
        }
    }
}
```

---

## Error Recovery

### Retry with Exponential Backoff

```csharp
public async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (ApiExecutionException ex) when (IsRetryableError(ex))
        {
            if (attempt == maxRetries)
                throw;
            
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
            Console.WriteLine($"Attempt {attempt} failed, retrying in {delay.TotalSeconds}s...");
            await Task.Delay(delay);
        }
    }
    
    throw new InvalidOperationException("All retry attempts failed");
}

private bool IsRetryableError(ApiExecutionException ex)
{
    // Retry on network errors and 5xx status codes
    return ex.InnerException is HttpRequestException ||
           ex.InnerException is TaskCanceledException ||
           (ex.StatusCode >= 500 && ex.StatusCode < 600);
}

// Usage
var data = await ExecuteWithRetry(() => 
    api.For("/data")
        .Get()
        .ShouldReturn<Data>()
);
```

### Fallback Strategies

```csharp
public async Task<Data> GetDataWithFallback()
{
    try
    {
        // Try primary endpoint
        return await api.For("/api/v1/data")
            .Get()
            .ShouldReturn<Data>();
    }
    catch (ApiExecutionException ex) when (ex.StatusCode == 404)
    {
        try
        {
            // Fallback to legacy endpoint
            return await api.For("/api/legacy/data")
                .Get()
                .ShouldReturn<Data>();
        }
        catch (ApiExecutionException)
        {
            // Return cached data or default
            return GetCachedData();
        }
    }
}
```

### Circuit Breaker Pattern

```csharp
public class CircuitBreakerApi
{
    private readonly IApi _api;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime;

    public async Task<T> Execute<T>(Func<Task<T>> operation)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > TimeSpan.FromMinutes(1))
            {
                _state = CircuitBreakerState.HalfOpen;
            }
            else
            {
                throw new InvalidOperationException("Circuit breaker is open");
            }
        }

        try
        {
            var result = await operation();
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
            return result;
        }
        catch (ApiExecutionException)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_failureCount >= 5)
            {
                _state = CircuitBreakerState.Open;
            }
            
            throw;
        }
    }
}
```

---

## Best Practices

### 1. Use Specific Exception Handling

```csharp
try
{
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>();
}
catch (ApiAssertionException ex) when (ex.ActualStatusCode == 404)
{
    // Handle user not found
    Console.WriteLine("User not found");
}
catch (ApiAssertionException ex) when (ex.ActualStatusCode == 401)
{
    // Handle authentication required
    Console.WriteLine("Authentication required");
}
catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
{
    // Handle timeout
    Console.WriteLine("Request timed out");
}
catch (ApiExecutionException ex)
{
    // Handle other execution errors
    Console.WriteLine($"Request failed: {ex.Message}");
}
```

### 2. Provide Meaningful Error Messages

```csharp
try
{
    var order = await api.For("/orders/123")
        .Get()
        .ShouldReturn<Order>(body: o => o.Status == OrderStatus.Confirmed);
}
catch (ApiAssertionException ex)
{
    throw new InvalidOperationException(
        $"Order validation failed: Expected status 'Confirmed', " +
        $"but order {ex.ActualStatusCode} has different status. " +
        $"Response: {ex.ResponseBody}", ex);
}
```

### 3. Log Errors Appropriately

```csharp
public async Task<Data> GetDataSafely()
{
    try
    {
        return await api.For("/data")
            .Get()
            .ShouldReturn<Data>();
    }
    catch (ApiExecutionException ex)
    {
        _logger.LogError(ex, "Failed to get data from API. Status: {StatusCode}", ex.StatusCode);
        throw;
    }
    catch (ApiAssertionException ex)
    {
        _logger.LogWarning("Data validation failed: {Message}", ex.Message);
        throw;
    }
}
```

### 4. Use Timeout Appropriately

```csharp
// Short timeout for health checks
var health = await api.For("/health")
    .WithTimeout(TimeSpan.FromSeconds(5))
    .Get()
    .ShouldReturn<HealthStatus>();

// Longer timeout for data processing
var result = await api.For("/process")
    .WithTimeout(TimeSpan.FromMinutes(5))
    .Post(processData)
    .ShouldReturn<ProcessResult>();
```

### 5. Handle Partial Failures

```csharp
public async Task<List<User>> GetUsersWithPartialFailureHandling()
{
    var users = new List<User>();
    var errors = new List<Exception>();
    
    for (int i = 1; i <= 10; i++)
    {
        try
        {
            var user = await api.For($"/users/{i}")
                .Get()
                .ShouldReturn<User>();
            users.Add(user);
        }
        catch (ApiAssertionException ex) when (ex.ActualStatusCode == 404)
        {
            // User doesn't exist, skip
            continue;
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }
    }
    
    if (errors.Any())
    {
        _logger.LogWarning("Failed to get {Count} users: {Errors}", 
            errors.Count, string.Join(", ", errors.Select(e => e.Message)));
    }
    
    return users;
}
```

### 6. Validate Error Responses

```csharp
try
{
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>();
}
catch (ApiExecutionException ex)
{
    // Check if the error response contains useful information
    if (!string.IsNullOrEmpty(ex.ResponseBody))
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(ex.ResponseBody);
            Console.WriteLine($"API Error: {errorResponse.Message}");
            Console.WriteLine($"Error Code: {errorResponse.Code}");
        }
        catch (JsonException)
        {
            Console.WriteLine($"Raw error response: {ex.ResponseBody}");
        }
    }
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic error handling concepts
- **[Assertions](assertions.md)** - Response validation and assertion patterns
- **[Testing Guide](testing-guide.md)** - Testing error scenarios
- **[Troubleshooting](troubleshooting.md)** - Common error scenarios and solutions
- **[Examples](examples.md)** - Real-world error handling scenarios
- **[Configuration](configuration.md)** - Timeout and retry configuration
- **[Request Building](request-building.md)** - Timeout configuration
- **[API Reference](api-reference.md)** - Exception class documentation
