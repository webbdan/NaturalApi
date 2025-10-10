## **Architecture and Implementation Design**

This section defines the structure and mechanics behind the fluent API. The goal is to keep the public syntax effortless while the internals handle complexity in well-bounded modules.

The key design rule: **no cleverness leaks to the surface.**

---

### **1. Architectural Overview**

At its core, the library is divided into four layers:

```
Fluent DSL
│
├── ApiFacade (entry point)
│
├── Context Builders (For, With, Using)
│
├── Execution Engine (HTTP handling, deserialisation)
│
└── Validation Layer (assertions, error reporting)
```

Each layer is responsible for a single concern and communicates with the next through clear interfaces.

---

### **2. Core Interfaces**

#### `IApi`

The root entry point. It exposes `For()` and acts as a factory.

```csharp
public interface IApi
{
    IApiContext For(string endpoint);
}
```

#### `NaturalApiConfiguration`

A fluent configuration class that provides factory methods for common NaturalApi setup scenarios.

```csharp
public class NaturalApiConfiguration
{
    public string? BaseUrl { get; set; }
    public string? HttpClientName { get; set; }
    public IApiAuthProvider? AuthProvider { get; set; }

    // Factory methods for common scenarios
    public static NaturalApiConfiguration WithBaseUrl(string baseUrl);
    public static NaturalApiConfiguration WithAuth(IApiAuthProvider authProvider);
    public static NaturalApiConfiguration WithBaseUrlAndAuth(string baseUrl, IApiAuthProvider authProvider);
    public static NaturalApiConfiguration WithHttpClient(string httpClientName);
    public static NaturalApiConfiguration WithHttpClientAndAuth(string httpClientName, IApiAuthProvider authProvider);
}
```

This configuration class is designed to work seamlessly with dependency injection and provides a clean, readable way to configure NaturalApi instances.

#### `IApiContext`

The builder state before execution. Holds all the configuration details and composes new contexts fluently.

```csharp
public interface IApiContext
{
    IApiContext WithHeader(string key, string value);
    IApiContext WithHeaders(IDictionary<string,string> headers);
    IApiContext WithQueryParam(string key, object value);
    IApiContext WithQueryParams(object parameters);
    IApiContext WithPathParam(string key, object value);
    IApiContext WithPathParams(object parameters);
    IApiContext UsingAuth(string schemeOrToken);
    IApiContext UsingToken(string token);
    IApiContext AsUser(string username, string password);  // New method for username/password auth
    IApiContext WithTimeout(TimeSpan timeout);
    IApiResultContext Get();
    IApiResultContext Delete();
    IApiResultContext Post(object? body = null);
    IApiResultContext Put(object? body = null);
    IApiResultContext Patch(object? body = null);
}
```

#### `ServiceCollectionExtensions`

Comprehensive dependency injection support with 8 different registration patterns.

```csharp
public static class ServiceCollectionExtensions
{
    // Basic registration
    public static IServiceCollection AddNaturalApi(this IServiceCollection services);
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, NaturalApiConfiguration config);
    
    // With authentication
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, TAuth authProvider);
    public static IServiceCollection AddNaturalApiWithBaseUrl<TAuth>(this IServiceCollection services, string apiBaseUrl, TAuth authProvider);
    
    // With named HttpClient
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, string httpClientName);
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, string httpClientName, TAuth authProvider);
    
    // With factory
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Func<IServiceProvider, IApi> apiFactory);
    public static IServiceCollection AddNaturalApi<TAuth, TApi>(this IServiceCollection services, string httpClientName, Func<IServiceProvider, TAuth> authProviderFactory, Func<IServiceProvider, TApi> apiFactory);
}
```

#### `IApiResultContext`

Represents the executed call, exposing both the raw response and fluent validation methods.

```csharp
public interface IApiResultContext
{
    HttpResponseMessage Response { get; }
    int StatusCode { get; }
    IDictionary<string,string> Headers { get; }
    string RawBody { get; }
    T BodyAs<T>();
    IApiResultContext ShouldReturn<T>(
        int? status = null,
        Action<T>? bodyValidator = null,
        Func<IDictionary<string,string>, bool>? headers = null);
    IApiResultContext Then(Action<IApiResultContext> next);
}
```

---

### **3. Fluent API Implementation**

The fluent surface is built using small immutable context objects. Each chain method returns a new context with accumulated state rather than mutating the original.
This guarantees thread safety and predictable behaviour across async tests.

Example skeleton:

```csharp
internal sealed class ApiContext : IApiContext
{
    private readonly ApiRequestSpec _spec;
    private readonly IHttpExecutor _executor;

    public ApiContext(ApiRequestSpec spec, IHttpExecutor executor)
    {
        _spec = spec;
        _executor = executor;
    }

    public IApiContext WithHeader(string key, string value) =>
        new ApiContext(_spec.WithHeader(key, value), _executor);

    public IApiResultContext Get() => 
        _executor.Execute(_spec.WithMethod(HttpMethod.Get));
}
```

The `ApiRequestSpec` is a record storing the evolving state — URL, headers, params, body, etc.

---

### **4. The Execution Engine**

All HTTP logic lives behind `IHttpExecutor`.

```csharp
public interface IHttpExecutor
{
    IApiResultContext Execute(ApiRequestSpec spec);
}
```

Responsibilities:

* Build the full URL with query and path replacements.
* Prepare `HttpRequestMessage` with headers, body, and auth.
* Send the request via `HttpClient`.
* Capture response, normalise headers, and deserialise body.
* Return a hydrated `ApiResultContext`.

No fluent layer should ever touch `HttpClient` directly.

---

### **5. Validation Layer**

Assertions are handled by a dedicated `IApiValidator`, decoupled from both the DSL and HTTP handling.

```csharp
public interface IApiValidator
{
    void ValidateStatus(HttpResponseMessage response, int expected);
    void ValidateHeaders(HttpResponseMessage response, Func<IDictionary<string,string>,bool> predicate);
    void ValidateBody<T>(string rawBody, Action<T> validator);
}
```

`ShouldReturn` delegates all checking to this validator, which throws a well-formed `ApiAssertionException` containing:

* The failed expectation
* Actual values
* Endpoint and HTTP verb
* Optional snippet of the response body

This isolates assertion formatting from API flow.

---

### **6. Configuration and DI**

The entire engine can run standalone or via dependency injection.

Default configuration should allow:

```csharp
services.AddApiTester(options => {
    options.BaseUrl = "https://api.test.local";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders = new() { ["Accept"] = "application/json" };
});
```

This registers:

* `IApi` (entry facade)
* `IHttpExecutor`
* `IApiValidator`
* Shared `HttpClientFactory` instance

The fluent chain itself is stateless and disposable-free.

---

### **7. Extensibility Model**

To add new capabilities (e.g. `.UsingProxy()`, `.WithCookie()`, `.LogTo()`), extension methods can be defined on `IApiContext` without changing the core types.

Example:

```csharp
public static class ApiContextLoggingExtensions
{
    public static IApiContext LogTo(this IApiContext ctx, IReporter reporter)
        => new LoggingApiContext(ctx, reporter);
}
```

This avoids bloating the base interface and keeps feature creep isolated.

---

### **8. Error Reporting and Diagnostics**

When a validation fails, it should produce structured, human-readable output:

```
Request: POST /users
Expected status: 201
Actual status: 500
Body snippet: {"error":"Invalid name"}
Assertion: body.Name == "Dan"
```

Failures are *data*, not *exceptions*.
They can be surfaced through console output, a test reporter, or integrated with frameworks like NUnit or xUnit via custom assertions.

---

### **9. Thread Safety and Parallelism**

Because contexts are immutable, multiple requests can run concurrently without interference.
Each chain is an isolated unit — perfect for parallel API load testing or data setup within test suites.

---

### **10. Internal Implementation Details**

#### **Request Flow Internals**

The complete request flow from user code to HTTP execution:

```csharp
// 1. User calls Api.For()
var context = api.For("/users/{id}");

// 2. Api.For() creates ApiRequestSpec and ApiContext
public IApiContext For(string endpoint)
{
    var spec = new ApiRequestSpec(
        endpoint,
        HttpMethod.Get, // Default method
        _defaults?.DefaultHeaders ?? new Dictionary<string, string>(),
        new Dictionary<string, object>(), // Query params
        new Dictionary<string, object>(), // Path params
        null, // Body
        _defaults?.Timeout
    );
    
    return new ApiContext(spec, _executor, _defaults?.AuthProvider);
}

// 3. User chains configuration methods
context = context.WithPathParam("id", 123)
                 .WithHeader("Accept", "application/json")
                 .UsingAuth("Bearer token");

// 4. Each method returns new ApiContext with updated spec
public IApiContext WithPathParam(string key, object value)
{
    var newSpec = _spec.WithPathParam(key, value);
    return new ApiContext(newSpec, _executor, _authProvider);
}

// 5. User calls HTTP verb method
var result = context.Get();

// 6. ApiContext.Get() executes the request
public IApiResultContext Get()
{
    var spec = _spec.WithMethod(HttpMethod.Get);
    return ExecuteWithAuth(spec);
}

// 7. ExecuteWithAuth handles authentication
private IApiResultContext ExecuteWithAuth(ApiRequestSpec spec)
{
    if (_authProvider != null && !spec.SuppressAuth)
    {
        var token = _authProvider.GetAuthTokenAsync(spec.Username).GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(token))
        {
            spec = spec.WithHeader("Authorization", $"Bearer {token}");
        }
    }
    
    return _executor.Execute(spec);
}
```

#### **HttpClientExecutor Internals**

```csharp
public class HttpClientExecutor : IHttpExecutor
{
    private readonly HttpClient _httpClient;

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        // 1. Build URL with path and query parameters
        var url = BuildUrl(spec);
        
        // 2. Create HttpRequestMessage
        var request = new HttpRequestMessage(spec.Method, url);
        
        // 3. Add headers
        foreach (var header in spec.Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
        
        // 4. Add body if present
        if (spec.Body != null)
        {
            var json = JsonSerializer.Serialize(spec.Body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        // 5. Execute request
        var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
        
        // 6. Create result context
        return new ApiResultContext(response, this);
    }

    private string BuildUrl(ApiRequestSpec spec)
    {
        var url = spec.Endpoint;
        
        // Replace path parameters
        foreach (var param in spec.PathParams)
        {
            url = url.Replace($"{{{param.Key}}}", param.Value.ToString());
        }
        
        // Add query parameters
        if (spec.QueryParams.Any())
        {
            var queryString = string.Join("&", spec.QueryParams.Select(kvp => 
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}"));
            url += $"?{queryString}";
        }
        
        return url;
    }
}
```

#### **Authentication Integration**

The authentication system supports both traditional token-based authentication and the new username/password authentication via the `AsUser()` method.

```csharp
public class AuthenticatedHttpClientExecutor : IAuthenticatedHttpExecutor
{
    private readonly HttpClient _httpClient;

    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, string? password, bool suppressAuth)
    {
        // 1. Build base request
        var request = BuildRequest(spec);
        
        // 2. Add authentication if not suppressed
        if (!suppressAuth && authProvider != null)
        {
            // Support both username-only and username/password authentication
            var token = await authProvider.GetAuthTokenAsync(username, password);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        
        // 3. Execute request
        var response = await _httpClient.SendAsync(request);
        
        // 4. Return result context
        return new ApiResultContext(response, this);
    }
}
```

#### **Username/Password Authentication Flow**

The new `AsUser(username, password)` method provides a simplified authentication flow:

```csharp
public class ApiContext : IApiContext
{
    private string? _username;
    private string? _password;

    public IApiContext AsUser(string username, string password)
    {
        _username = username;
        _password = password;
        return this;
    }

    public IApiResultContext Get()
    {
        var spec = new ApiRequestSpec(/* ... */);
        return _executor.ExecuteAsync(spec, _authProvider, _username, _password, _suppressAuth);
    }
}
```

This design allows auth providers to handle both traditional token-based authentication and username/password authentication seamlessly.

#### **Validation Internals**

```csharp
public class ApiResultContext : IApiResultContext
{
    private readonly HttpResponseMessage _response;
    private readonly IHttpExecutor _executor;
    private string? _rawBody;

    public IApiResultContext ShouldReturn<T>(int? status = null, Action<T>? bodyValidator = null, Func<IDictionary<string, string>, bool>? headers = null)
    {
        // 1. Validate status code
        if (status.HasValue)
        {
            if ((int)_response.StatusCode != status.Value)
            {
                throw new ApiAssertionException(
                    $"Expected status {status.Value} but got {(int)_response.StatusCode} for {_response.RequestMessage?.Method} {_response.RequestMessage?.RequestUri}",
                    _response
                );
            }
        }
        
        // 2. Validate headers
        if (headers != null)
        {
            var responseHeaders = _response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            if (!headers(responseHeaders))
            {
                throw new ApiAssertionException(
                    $"Header validation failed for {_response.RequestMessage?.Method} {_response.RequestMessage?.RequestUri}",
                    _response
                );
            }
        }
        
        // 3. Validate body
        if (bodyValidator != null)
        {
            var body = BodyAs<T>();
            bodyValidator(body);
        }
        
        return this;
    }

    public T BodyAs<T>()
    {
        if (_rawBody == null)
        {
            _rawBody = _response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        
        return JsonSerializer.Deserialize<T>(_rawBody) ?? throw new ApiAssertionException("Response body is null");
    }
}
```

#### **ServiceCollectionExtensions Internals**

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Action<NaturalApiOptions>? configureDefaults = null)
    {
        // Register HttpClient
        services.AddHttpClient();
        
        // Register default implementations
        services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        services.AddSingleton<IHttpExecutor, HttpClientExecutor>();
        services.AddSingleton<IApiValidator, ApiValidator>();
        
        // Register Api factory
        services.AddSingleton<IApi>(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
            var executor = provider.GetRequiredService<IHttpExecutor>();
            return new Api(executor, defaults);
        });
        
        // Configure options
        if (configureDefaults != null)
        {
            services.Configure(configureDefaults);
        }
        
        return services;
    }
}
```

#### **Error Handling Internals**

```csharp
public class ApiAssertionException : Exception
{
    public int ExpectedStatusCode { get; }
    public int ActualStatusCode { get; }
    public string ResponseBody { get; }
    public IDictionary<string, string> Headers { get; }

    public ApiAssertionException(string message, HttpResponseMessage response) : base(message)
    {
        ActualStatusCode = (int)response.StatusCode;
        ResponseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
    }
}
```

#### **Thread Safety Implementation**

```csharp
// All context objects are immutable
public record ApiRequestSpec
{
    public string Endpoint { get; init; }
    public HttpMethod Method { get; init; }
    public IDictionary<string, string> Headers { get; init; }
    public IDictionary<string, object> QueryParams { get; init; }
    public IDictionary<string, object> PathParams { get; init; }
    public object? Body { get; init; }
    public TimeSpan? Timeout { get; init; }
    public IDictionary<string, string> Cookies { get; init; }
    public string? Username { get; init; }
    public bool SuppressAuth { get; init; }

    // Fluent methods return new instances
    public ApiRequestSpec WithHeader(string key, string value) =>
        this with { Headers = Headers.Concat(new[] { new KeyValuePair<string, string>(key, value) }).ToDictionary() };
}
```

---

### **11. Summary**

| Layer                 | Responsibility                                                |
| --------------------- | ------------------------------------------------------------- |
| **Fluent DSL**        | User-facing grammar; reads like natural language.             |
| **ApiContext**        | Immutable builder managing request specification.             |
| **IHttpExecutor**     | Executes HTTP calls and normalises results.                   |
| **IApiValidator**     | Validates response and throws readable exceptions.            |
| **IApiResultContext** | Wraps results and provides validation + chaining.             |
| **DI Layer**          | Central configuration, shared HttpClient, plug-in extensions. |

---

## **Related Topics**

- [**Getting Started**](getting-started.md) - Installation, first API call, basic setup
- [**Configuration**](configuration.md) - Base URLs, timeouts, default headers, DI setup
- [**Dependency Injection Guide**](di.md) - DI patterns and ServiceCollectionExtensions
- [**Extensibility**](extensibility.md) - Custom executors, validators, auth providers
- [**API Reference**](api-reference.md) - Complete interface and class documentation
- [**Testing Guide**](testing-guide.md) - Unit testing with mocks, integration testing
- [**Examples**](examples.md) - Real-world scenarios and complete examples
- [**Philosophy & Design Principles**](philosophyanddesignprinciples.md) - Core design philosophy
- [**Fluent Syntax Reference**](fluentsyntax.md) - Complete grammar and method reference
- [**Contributing**](contributing.md) - Architecture internals and contribution guidelines
