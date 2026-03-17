# Api Facade Design

This document describes the facade pattern used in `UsingFacadeAbstraction_ShouldSupportMultipleSteps()` and explains the design paradigms, simple usage, reasoning and benefits of introducing a small `Api` facade layer (a thin service) on top of the NaturalApi DSL.

## Purpose

The facade (aka service) wraps repetitive NaturalApi usage patterns for a given resource (for example `/posts`), providing a concise, strongly-typed surface for tests and application code. The tests in `Multistep.cs` show how a `PostsService` is used to hide repeated `Api.For("/posts")` call sites and to return domain models directly.

## Design paradigms

- Facade Pattern: Provide a simple interface for the common operations over a resource (List, Get, Create, Update, Delete). The facade internally delegates to `Api` and the NaturalApi fluent DSL.
- Single Responsibility: The facade focuses on mapping a resource to requests; it does not perform business logic or persistence beyond orchestrating HTTP calls.
- Composition over Inheritance: The facade holds an `IApi` (or `Api`) instance and composes calls rather than subclassing the API runtime.
- Dependency Injection friendly: Accept `IApi` or an `HttpClient` via constructor so tests can substitute mocks or test servers easily.
- Immutability of request contexts: Continue to rely on NaturalApi's immutable `ApiContext` chain semantics. The facade creates short-lived contexts per call.

## Simple usage (test example)

From the test `UsingFacadeAbstraction_ShouldSupportMultipleSteps()`:

- Construction
  - `var postsApi = new PostsService("https://jsonplaceholder.typicode.com");`

- Typical calls
  - `var posts = postsApi.List();`
  - `var post = postsApi.Create(new Post { title = "foo", body = "bar", userId = 1 });`
  - `var singlePost = postsApi.Get(1);`

The facade methods return concrete domain types (`Post`, `List<Post>`), hiding the `.For(...).Get().ShouldReturn<T>()` plumbing.

## Example facade shape

A recommended minimal shape for the facade (conceptual, omit error handling for clarity):

`PostsService`
- Constructor accepts `string baseUrl` or `IApi`/`HttpClient`.
- Methods:
  - `List(): List<Post>` -> calls `Api.For("/posts").Get().ShouldReturn<List<Post>>()`
  - `Get(int id): Post` -> calls `Api.For("/posts/{id}").WithPathParam("id", id).Get().ShouldReturn<Post>()`
  - `Create(PostCreate request): Post` -> posts `request` and returns the created `Post`

This keeps call sites concise and intent-focused.

## Reasoning and benefits

- Readability: Tests read as plain English. `postsApi.Create(...)` is shorter and clearer than building the entire request inline.
- DRY (Don’t Repeat Yourself): Common configuration (base URL, default headers, content-type) is defined once in the service. If the API shape or base path changes, only the facade needs updating.
- Strong typing: Facade returns domain models; deserialization happens in one place. Mismatches are easier to find and fix.
- Encapsulation: Error handling, response mapping, and common transformations (e.g. mapping API DTOs to domain objects) live in the service instead of distributed across tests.
- Easier test maintenance: Tests focus on behaviour; the facade surface is smaller to keep stable.
- Reuse outside tests: The same facade can be used by other test suites or lightweight integration code.

## Implementation considerations

- Thread safety: Make facades stateless or use thread-safe singletons. Prefer injecting an `IApi` or `HttpClient` rather than storing mutable state.
- Timeouts and retries: Centralize timeout and retry policy in the facade or in the configured `HttpClient`.
- Error mapping: Map HTTP error codes to meaningful exceptions or result types at the facade boundary to avoid test duplication.
- Serialization: Use consistent `JsonSerializerOptions` (camelCase, case-insensitive) when you manually (de)serialize bodies.
- Logging / reporting: Facades can attach a reporter (the `INaturalReporter`) or wrap calls with logging for better diagnostics in CI.
- Test doubles: Make the constructor accept `IApi` or a factory to enable unit tests to inject a `MockHttpExecutor` or a `WireMock` client.

## Example DI-friendly constructor patterns

- Accept base URL and create `Api` internally:
  - `public PostsService(string baseUrl) { _api = new Api(new HttpClientExecutor(new HttpClient { BaseAddress = new Uri(baseUrl) })); }`
- Accept `IApi` directly (preferred for testability):
  - `public PostsService(IApi api) { _api = api; }`

## Common pitfalls to avoid

- Storing per-request mutable state on the facade instance (e.g. last response). This undermines thread-safety.
- Returning raw `IApiResultContext` from the facade — prefer deserialized domain objects or a small `Result<T>` wrapper.
- Duplicating header or auth logic across many small facades — centralize default headers/auth in a `IApiDefaultsProvider` or at factory time.

## Conclusion

A concise `Api` facade reduces repeated boilerplate in tests and keeps test code focused on behaviour. It improves readability, maintainability and testability while keeping the expressive NaturalApi DSL at the implementation surface. Keep the facade small, stateless, DI-friendly and responsible for mapping resource operations to typed results.


## DI-friendly service: conceptual design

This section shows a compact, DI-friendly conceptual design for a facade that integrates cleanly with ASP.NET Core dependency injection and is easy to unit test.

### Interface

```csharp
public interface IPostsService
{
    List<Post> List();
    Post Get(int id);
    Post Create(PostCreate request);
}
```

### Minimal implementation (DI-friendly)

```csharp
public class PostsService : IPostsService
{
    private readonly IApi _api;

    // Preferred for testability: accept IApi (can be created from HttpClient in DI setup)
    public PostsService(IApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public List<Post> List()
    {
        return _api.For("/posts").Get().ShouldReturn<List<Post>>();
    }

    public Post Get(int id)
    {
        return _api.For("/posts/{id}").WithPathParam("id", id).Get().ShouldReturn<Post>();
    }

    public Post Create(PostCreate request)
    {
        return _api.For("/posts").Post(request).ShouldReturn<Post>();
    }
}
```

### DI registration examples

- If you register an `IApi` that uses a named or configured `HttpClient`, register the facade as transient or scoped:

```csharp
// Register HttpClient for the API
services.AddHttpClient("PostsApiClient", client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
});

// Register IApi using the named HttpClient
services.AddSingleton<IApi>(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("PostsApiClient");
    return new Api(new HttpClientExecutor(client));
});

// Register the facade (stateless) as transient
services.AddTransient<IPostsService, PostsService>();
```

- Alternatively register the facade to create its own Api with an injected `HttpClient`:

```csharp
services.AddHttpClient<IPostsService, PostsService>(client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
});

// PostsService constructor in this case should accept HttpClient and construct Api
public PostsService(HttpClient httpClient)
{
    var executor = new HttpClientExecutor(httpClient);
    _api = new Api(executor);
}
```

### Lifetime guidance

- `HttpClient` should be managed by `IHttpClientFactory` (AddHttpClient) to avoid socket exhaustion.
- `IApi` can be registered as singleton if it wraps a long-lived `HttpClient` and is stateless.
- Facades can be `Transient` or `Scoped`. Prefer `Transient` when they are thin and stateless; choose `Scoped` if they will hold scoped services.

### Testability patterns

- Unit tests: inject a `MockHttpExecutor` using a fake `IApi` (create `Api` with a `MockHttpExecutor` or inject a test double for `IApi`):

```csharp
var mockExecutor = new MockHttpExecutor();
var api = new Api(mockExecutor);
var posts = new PostsService(api);
```

- Integration tests: register `IPostsService` in the test's `IServiceCollection` pointing at a test server (WireMock or in-process listener) using `AddHttpClient`.

### Error handling and mapping

Wrap HTTP/serialization errors in small domain exceptions at the facade boundary to simplify test assertions and avoid spreading HTTP specifics into test code.

```csharp
try
{
    return _api.For("/posts/{id}").WithPathParam("id", id).Get().ShouldReturn<Post>();
}
catch (ApiAssertionException ex)
{
    throw new PostsNotFoundException(id, ex);
}
```

This DI-friendly conceptual design keeps the facade testable, composable in DI, and production-ready while preserving the simplicity tests require.


## Using the `ApiFacade` base class (how it makes code trivial)

The repository contains a small, reusable `ApiFacade` base class that further reduces ceremony for resource facades such as `PostsFacade`.

### What `ApiFacade` does

- Holds a typed `Api` instance and exposes a `BaseRoute` that child facades override.
- Provides generic helper methods (`Get<T>`, `Post<TBody,T>`, etc.) that combine the `BaseRoute` with a child-provided route fragment and call NaturalApi under the covers.
- Centralises simple route combination and common plumbing, so child classes only declare intent (List, Get, Create) and return domain types.

### Example: `PostsFacade` (already present in code)

```csharp
public class PostsFacade : ApiFacade
{
    protected override string BaseRoute => "/posts";

    public PostsFacade(Api api) : base(api) { }

    public List<Post> List() => Get<List<Post>>();

    public Post Get(int id) => Get<Post>(id.ToString());

    public Post Create(Post model) => Post<Post, Post>(model);
}
```

This small class replaces repeated calls like:

```csharp
_api.For("/posts").Get().ShouldReturn<List<Post>>();
_api.For("/posts/{id}").WithPathParam("id", id).Get().ShouldReturn<Post>();
_api.For("/posts").Post(model).ShouldReturn<Post>();
```

with single-line intent methods: `List()`, `Get(id)`, `Create(model)`.

### Where to extend the base class

- Add `WithHeader`, `WithQueryParam` helper overloads on the base facade to support common headers or query parameters consistently.
- Add `Put<TBody,T>` and `Delete<T>` helpers for full CRUD.
- Add protected hooks for transforming request/response (e.g. map DTO -> domain) in one place.

### Benefits

- Tests are more readable and focused on behaviour, not plumbing.
- Changing routes, headers, auth or default query params is a single edit in the facade base or child class.
- Small, focused classes are easier to mock and reason about in unit tests.

### Example of adding a simple header helper in `ApiFacade`

```csharp
protected T GetWithHeader<T>(string headerName, string headerValue, string route = "")
    => Api.For(Combine(route)).WithHeader(headerName, headerValue).Get().ShouldReturn<T>();
```

Child facades can call `GetWithHeader<T>` to reuse the behaviour without repeating header logic.

### When not to use this pattern

- Complex orchestration flows where the facade would need large amounts of business logic — prefer a service layer that composes facades and other collaborators.
- When tests deliberately exercise the request-building DSL; in that case keep the direct `Api` calls in the test for clarity.

Overall, the `ApiFacade` + small child facades (like `PostsFacade`) keep tests concise and intent-driven while centralising HTTP plumbing where it belongs.
