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
    IApiContext WithTimeout(TimeSpan timeout);
    IApiResultContext Get();
    IApiResultContext Delete();
    IApiResultContext Post(object? body = null);
    IApiResultContext Put(object? body = null);
    IApiResultContext Patch(object? body = null);
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

### **10. Summary**

| Layer                 | Responsibility                                                |
| --------------------- | ------------------------------------------------------------- |
| **Fluent DSL**        | User-facing grammar; reads like natural language.             |
| **ApiContext**        | Immutable builder managing request specification.             |
| **IHttpExecutor**     | Executes HTTP calls and normalises results.                   |
| **IApiValidator**     | Validates response and throws readable exceptions.            |
| **IApiResultContext** | Wraps results and provides validation + chaining.             |
| **DI Layer**          | Central configuration, shared HttpClient, plug-in extensions. |
