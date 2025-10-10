# AIModified:2025-10-10T18:34:01Z
# API Reference

> Complete reference documentation for all NaturalApi interfaces, classes, and methods.

---

## Table of Contents

- [Core Interfaces](#core-interfaces)
- [Context Interfaces](#context-interfaces)
- [Execution Interfaces](#execution-interfaces)
- [Authentication Interfaces](#authentication-interfaces)
- [Configuration Classes](#configuration-classes)
- [Configuration Interfaces](#configuration-interfaces)
- [Validation Interfaces](#validation-interfaces)
- [Exception Classes](#exception-classes)
- [Service Collection Extensions](#service-collection-extensions)

---

## Core Interfaces

### IApi

Main entry point for the NaturalApi fluent DSL.

```csharp
public interface IApi
{
    /// <summary>
    /// Creates a new API context for the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
    /// <returns>An API context for building and executing requests</returns>
    IApiContext For(string endpoint);
}
```

**Usage:**
```csharp
var api = new Api("https://api.example.com");
var context = api.For("/users");
```

---

## Configuration Classes

### NaturalApiConfiguration

A fluent configuration class that provides factory methods for common NaturalApi setup scenarios.

```csharp
public class NaturalApiConfiguration
{
    /// <summary>
    /// The base URL for the API. If not provided, absolute URLs must be used in For() calls.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The name of the HttpClient to use. If not provided, a default HttpClient will be used.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// The auth provider to use for authentication.
    /// </summary>
    public IApiAuthProvider? AuthProvider { get; set; }

    /// <summary>
    /// Creates a simple configuration with just a base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API</param>
    /// <returns>A configuration with the specified base URL</returns>
    public static NaturalApiConfiguration WithBaseUrl(string baseUrl);

    /// <summary>
    /// Creates a configuration with a named HttpClient.
    /// </summary>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <returns>A configuration with the specified HttpClient name</returns>
    public static NaturalApiConfiguration WithHttpClient(string httpClientName);

    /// <summary>
    /// Creates a configuration with an auth provider.
    /// </summary>
    /// <param name="authProvider">The auth provider to use</param>
    /// <returns>A configuration with the specified auth provider</returns>
    public static NaturalApiConfiguration WithAuth(IApiAuthProvider authProvider);

    /// <summary>
    /// Creates a configuration with both base URL and auth provider.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API</param>
    /// <param name="authProvider">The auth provider to use</param>
    /// <returns>A configuration with the specified base URL and auth provider</returns>
    public static NaturalApiConfiguration WithBaseUrlAndAuth(string baseUrl, IApiAuthProvider authProvider);

    /// <summary>
    /// Creates a configuration with both HttpClient name and auth provider.
    /// </summary>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <param name="authProvider">The auth provider to use</param>
    /// <returns>A configuration with the specified HttpClient name and auth provider</returns>
    public static NaturalApiConfiguration WithHttpClientAndAuth(string httpClientName, IApiAuthProvider authProvider);
}
```

**Usage:**
```csharp
// Simple base URL configuration
var config = NaturalApiConfiguration.WithBaseUrl("https://api.example.com");

// With authentication
var config = NaturalApiConfiguration.WithBaseUrlAndAuth(
    "https://api.example.com", 
    myAuthProvider);

// With named HttpClient
var config = NaturalApiConfiguration.WithHttpClientAndAuth(
    "MyApiClient", 
    myAuthProvider);

// Use with dependency injection
services.AddNaturalApi(config);
```

---

## Context Interfaces

### IApiContext

Represents the builder state before execution. Holds all configuration details and composes new contexts fluently.

```csharp
public interface IApiContext
{
    // Header methods
    IApiContext WithHeader(string key, string value);
    IApiContext WithHeaders(IDictionary<string, string> headers);

    // Query parameter methods
    IApiContext WithQueryParam(string key, object value);
    IApiContext WithQueryParams(object parameters);

    // Path parameter methods
    IApiContext WithPathParam(string key, object value);
    IApiContext WithPathParams(object parameters);

    // Authentication methods
    IApiContext UsingAuth(string schemeOrToken);
    IApiContext UsingToken(string token);
    IApiContext WithoutAuth();
    IApiContext AsUser(string username, string password);

    // Configuration methods
    IApiContext WithTimeout(TimeSpan timeout);

    // Cookie methods
    IApiContext WithCookie(string name, string value);
    IApiContext WithCookies(IDictionary<string, string> cookies);
    IApiContext ClearCookies();

    // HTTP verb methods
    IApiResultContext Get();
    IApiResultContext Delete();
    IApiResultContext Post(object? body = null);
    IApiResultContext Put(object? body = null);
    IApiResultContext Patch(object? body = null);
}
```

**Usage:**
```csharp
// Traditional token authentication
var result = await api.For("/users")
    .WithHeader("Accept", "application/json")
    .WithQueryParam("page", 1)
    .WithPathParam("id", 123)
    .UsingAuth("Bearer token")
    .Get();

// New username/password authentication
var result = await api.For("/protected")
    .AsUser("myusername", "mypassword")
    .Get();
```

### IApiResultContext

Represents the executed call, exposing both the raw response and fluent validation methods.

```csharp
public interface IApiResultContext
{
    // Response properties
    HttpResponseMessage Response { get; }
    int StatusCode { get; }
    IDictionary<string, string> Headers { get; }
    string RawBody { get; }

    // Body deserialization
    T BodyAs<T>();

    // Validation methods
    IApiResultContext ShouldReturn<T>(
        int? status = null,
        Action<T>? bodyValidator = null,
        Func<IDictionary<string, string>, bool>? headers = null);

    IApiResultContext ShouldReturn(int status);
    IApiResultContext ShouldReturn<T>();
    IApiResultContext ShouldReturn<T>(int status);
    IApiResultContext ShouldReturn<T>(Action<T> bodyValidator);
    IApiResultContext ShouldReturn<T>(int status, Action<T> bodyValidator);
    IApiResultContext ShouldReturn<T>(int status, Action<T> bodyValidator, Func<IDictionary<string, string>, bool> headers);

    // Chaining
    IApiResultContext Then(Action<IApiResultContext> next);
}
```

**Usage:**
```csharp
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(status: 200, body: u => u.Id == 1);
```

---

## Execution Interfaces

### IHttpExecutor

Executes HTTP requests and returns result contexts. All HTTP logic lives behind this interface.

```csharp
public interface IHttpExecutor
{
    /// <summary>
    /// Executes an HTTP request based on the provided specification.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <returns>Result context with response data and validation methods</returns>
    IApiResultContext Execute(ApiRequestSpec spec);
}
```

**Usage:**
```csharp
var executor = new HttpClientExecutor(new HttpClient());
var result = executor.Execute(spec);
```

### IAuthenticatedHttpExecutor

HTTP executor that supports authentication resolution. Extends IHttpExecutor with authentication capabilities.

```csharp
public interface IAuthenticatedHttpExecutor : IHttpExecutor
{
    /// <summary>
    /// Executes an HTTP request with authentication resolution.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <param name="authProvider">Authentication provider for token resolution</param>
    /// <param name="username">Username context for per-user authentication</param>
    /// <param name="password">Password context for authentication</param>
    /// <param name="suppressAuth">Whether to suppress authentication for this request</param>
    /// <returns>Result context with response data and validation methods</returns>
    Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, string? password, bool suppressAuth);
}
```

**Usage:**
```csharp
var authExecutor = new AuthenticatedHttpClientExecutor(httpClient);
var result = await authExecutor.ExecuteAsync(spec, authProvider, "john", "password", false);
```

---

## Authentication Interfaces

### IApiAuthProvider

Contract for all authentication providers. Provides tokens to be automatically added to outgoing requests.

```csharp
public interface IApiAuthProvider
{
    /// <summary>
    /// Returns a valid auth token (without the scheme).
    /// Returning null means no auth header will be added.
    /// </summary>
    /// <param name="username">Optional username for per-user token resolution</param>
    /// <param name="password">Optional password for authentication</param>
    /// <returns>Authentication token without scheme, or null if no auth should be added</returns>
    Task<string?> GetAuthTokenAsync(string? username = null, string? password = null);
}
```

**Usage:**
```csharp
public class MyAuthProvider : IApiAuthProvider
{
    public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
    {
        // Return token for the specified user
        return await GetTokenForUserAsync(username, password);
    }
}
```

---

## Configuration Interfaces

### IApiDefaultsProvider

Provides default configuration for API requests including base URI, headers, timeout, and auth provider.

```csharp
public interface IApiDefaultsProvider
{
    /// <summary>
    /// Base URI for all requests. Can be null if not specified.
    /// </summary>
    Uri? BaseUri { get; }

    /// <summary>
    /// Default headers to be added to all requests.
    /// </summary>
    IDictionary<string, string> DefaultHeaders { get; }

    /// <summary>
    /// Default timeout for requests.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    /// Authentication provider for automatic token resolution.
    /// Can be null if no authentication is configured.
    /// </summary>
    IApiAuthProvider? AuthProvider { get; }
}
```

**Usage:**
```csharp
public class MyDefaultsProvider : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri("https://api.example.com");
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json"
    };
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider => null;
}
```

---

## Validation Interfaces

### IApiValidator

Handles API response validation and assertion logic. Decoupled from both the DSL and HTTP handling.

```csharp
public interface IApiValidator
{
    /// <summary>
    /// Validates the HTTP status code of a response.
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <param name="expected">Expected status code</param>
    /// <exception cref="ApiAssertionException">Thrown when status code doesn't match</exception>
    void ValidateStatus(HttpResponseMessage response, int expected);

    /// <summary>
    /// Validates response headers using a predicate.
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <param name="predicate">Function to validate headers</param>
    /// <exception cref="ApiAssertionException">Thrown when headers don't match predicate</exception>
    void ValidateHeaders(HttpResponseMessage response, Func<IDictionary<string, string>, bool> predicate);

    /// <summary>
    /// Validates response body using a custom validator function.
    /// </summary>
    /// <typeparam name="T">Type to deserialize body to</typeparam>
    /// <param name="rawBody">Raw response body string</param>
    /// <param name="validator">Function to validate the deserialized body</param>
    /// <exception cref="ApiAssertionException">Thrown when body validation fails</exception>
    void ValidateBody<T>(string rawBody, Action<T> validator);
}
```

**Usage:**
```csharp
public class MyValidator : IApiValidator
{
    public void ValidateStatus(HttpResponseMessage response, int expected)
    {
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
        validator(body);
    }
}
```

---

## Exception Classes

### ApiAssertionException

Thrown when response validation fails (status codes, body validation, headers).

```csharp
public class ApiAssertionException : Exception
{
    public int ExpectedStatusCode { get; }
    public int ActualStatusCode { get; }
    public string ResponseBody { get; }
    public IDictionary<string, string> Headers { get; }

    public ApiAssertionException(string message) : base(message) { }
    public ApiAssertionException(string message, Exception innerException) : base(message, innerException) { }
    public ApiAssertionException(string message, HttpResponseMessage response) : base(message) { }
}
```

**Usage:**
```csharp
try
{
    await api.For("/users/999")
        .Get()
        .ShouldReturn<User>(status: 200);
}
catch (ApiAssertionException ex)
{
    Console.WriteLine($"Expected {ex.ExpectedStatusCode}, got {ex.ActualStatusCode}");
}
```

### ApiExecutionException

Thrown when HTTP execution fails (network errors, timeouts, server errors).

```csharp
public class ApiExecutionException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }
    public IDictionary<string, string> Headers { get; }
    public string RequestUrl { get; }
    public HttpMethod HttpMethod { get; }

    public ApiExecutionException(string message) : base(message) { }
    public ApiExecutionException(string message, Exception innerException) : base(message, innerException) { }
}
```

**Usage:**
```csharp
try
{
    var data = await api.For("/unreachable")
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex)
{
    Console.WriteLine($"Request failed: {ex.Message}");
    Console.WriteLine($"Status: {ex.StatusCode}");
}
```

---

## Core Classes

### Api

Main entry point for the NaturalApi fluent DSL. Provides the For() method to create API contexts.

```csharp
public class Api : IApi
{
    // Constructors
    public Api(IHttpExecutor httpExecutor);
    public Api(string baseUrl);
    public Api(IHttpExecutor httpExecutor, IApiDefaultsProvider defaults);

    // Main method
    public IApiContext For(string endpoint);
}
```

**Usage:**
```csharp
// With base URL
var api = new Api("https://api.example.com");

// With executor
var api = new Api(new HttpClientExecutor(new HttpClient()));

// With defaults
var api = new Api(executor, defaults);
```

### ApiContext

Immutable context for building API requests. Each method returns a new context with accumulated state.

```csharp
public sealed class ApiContext : IApiContext
{
    // Constructor
    public ApiContext(ApiRequestSpec spec, IHttpExecutor executor);
    public ApiContext(ApiRequestSpec spec, IHttpExecutor executor, IApiAuthProvider? authProvider);

    // All IApiContext methods implemented
}
```

### ApiRequestSpec

Record storing the evolving request state — URL, headers, params, body, etc.

```csharp
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

    // Fluent methods
    public ApiRequestSpec WithHeader(string key, string value);
    public ApiRequestSpec WithHeaders(IDictionary<string, string> headers);
    public ApiRequestSpec WithQueryParam(string key, object value);
    public ApiRequestSpec WithQueryParams(object parameters);
    public ApiRequestSpec WithPathParam(string key, object value);
    public ApiRequestSpec WithPathParams(object parameters);
    public ApiRequestSpec WithBody(object? body);
    public ApiRequestSpec WithTimeout(TimeSpan timeout);
    public ApiRequestSpec WithCookie(string name, string value);
    public ApiRequestSpec WithCookies(IDictionary<string, string> cookies);
    public ApiRequestSpec ClearCookies();
    public ApiRequestSpec AsUser(string username);
    public ApiRequestSpec WithoutAuth();
    public ApiRequestSpec WithMethod(HttpMethod method);
}
```

### ApiResultContext

Wraps the executed call, exposing both the raw response and fluent validation methods.

```csharp
public class ApiResultContext : IApiResultContext
{
    // Constructor
    public ApiResultContext(HttpResponseMessage response, IHttpExecutor executor);

    // Properties
    public HttpResponseMessage Response { get; }
    public int StatusCode { get; }
    public IDictionary<string, string> Headers { get; }
    public string RawBody { get; }

    // Methods
    public T BodyAs<T>();
    public IApiResultContext ShouldReturn<T>(int? status = null, Action<T>? bodyValidator = null, Func<IDictionary<string, string>, bool>? headers = null);
    public IApiResultContext ShouldReturn(int status);
    public IApiResultContext ShouldReturn<T>();
    public IApiResultContext ShouldReturn<T>(int status);
    public IApiResultContext ShouldReturn<T>(Action<T> bodyValidator);
    public IApiResultContext ShouldReturn<T>(int status, Action<T> bodyValidator);
    public IApiResultContext ShouldReturn<T>(int status, Action<T> bodyValidator, Func<IDictionary<string, string>, bool> headers);
    public IApiResultContext Then(Action<IApiResultContext> next);
}
```

---

## HTTP Executors

### HttpClientExecutor

Standard HTTP executor using HttpClient.

```csharp
public class HttpClientExecutor : IHttpExecutor
{
    public HttpClientExecutor(HttpClient httpClient);
    public IApiResultContext Execute(ApiRequestSpec spec);
}
```

### AuthenticatedHttpClientExecutor

HttpClient-based implementation of IAuthenticatedHttpExecutor.

```csharp
public class AuthenticatedHttpClientExecutor : IAuthenticatedHttpExecutor
{
    public AuthenticatedHttpClientExecutor(HttpClient httpClient);
    public IApiResultContext Execute(ApiRequestSpec spec);
    public Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, bool suppressAuth);
}
```

---

## Default Implementations

### DefaultApiDefaults

Default implementation of IApiDefaultsProvider.

```csharp
public class DefaultApiDefaults : IApiDefaultsProvider
{
    public DefaultApiDefaults();
    public DefaultApiDefaults(Uri? baseUri, IDictionary<string, string>? defaultHeaders, TimeSpan? timeout, IApiAuthProvider? authProvider);

    public Uri? BaseUri { get; }
    public IDictionary<string, string> DefaultHeaders { get; }
    public TimeSpan Timeout { get; }
    public IApiAuthProvider? AuthProvider { get; }
}
```

### CachingAuthProvider

Example implementation of IApiAuthProvider that caches tokens and refreshes them when expired.

```csharp
public class CachingAuthProvider : IApiAuthProvider
{
    public CachingAuthProvider();
    public Task<string?> GetAuthTokenAsync(string? username = null, string? password = null);
}
```

---

## Service Collection Extensions

### ServiceCollectionExtensions

Extension methods for registering NaturalApi services with dependency injection.

```csharp
public static class ServiceCollectionExtensions
{
    // Basic registration
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Action<NaturalApiOptions>? configureDefaults = null);
    
    // With custom defaults
    public static IServiceCollection AddNaturalApi<TDefaults>(this IServiceCollection services, TDefaults defaultsProvider) where TDefaults : class, IApiDefaultsProvider;
    
    // With auth provider
    public static IServiceCollection AddNaturalApiWithAuth<TAuth>(this IServiceCollection services, TAuth authProvider, Action<NaturalApiOptions>? configureDefaults = null) where TAuth : class, IApiAuthProvider;
    
    // With both defaults and auth
    public static IServiceCollection AddNaturalApiWithAuth<TDefaults, TAuth>(this IServiceCollection services, TDefaults defaultsProvider, TAuth authProvider) where TDefaults : class, IApiDefaultsProvider where TAuth : class, IApiAuthProvider;
    
    // With custom factory
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Func<IServiceProvider, IApi> apiFactory);
}
```

### NaturalApiOptions

Configuration options for NaturalApi registration.

```csharp
public class NaturalApiOptions
{
    public bool RegisterDefaults { get; set; } = true;
}
```

**Usage:**
```csharp
// Basic registration
services.AddNaturalApi();

// With configuration
services.AddNaturalApi(options =>
{
    options.RegisterDefaults = true;
});

// With custom defaults
services.AddNaturalApi(new MyDefaultsProvider());

// With auth provider
services.AddNaturalApiWithAuth<MyAuthProvider>(new MyAuthProvider());

// With both defaults and auth
services.AddNaturalApiWithAuth<MyDefaults, MyAuth>(new MyDefaults(), new MyAuth());
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic usage examples
- **[Configuration](configuration.md)** - Setting up NaturalApi
- **[Authentication](authentication.md)** - Authentication patterns
- **[Request Building](request-building.md)** - Using IApiContext methods
- **[HTTP Verbs](http-verbs.md)** - Using HTTP verb methods
- **[Assertions](assertions.md)** - Using IApiResultContext validation
- **[Error Handling](error-handling.md)** - Exception handling
- **[Extensibility](extensibility.md)** - Creating custom implementations
- **[Testing Guide](testing-guide.md)** - Testing with NaturalApi
- **[Examples](examples.md)** - Real-world usage scenarios
- **[Fluent Syntax Reference](fluentsyntax.md)** - Complete method reference
- **[Dependency Injection Guide](di.md)** - ServiceCollectionExtensions usage
