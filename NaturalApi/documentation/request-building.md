# Request Building Guide

> NaturalApi provides a comprehensive set of methods for building HTTP requests. This guide covers headers, query parameters, path parameters, cookies, timeouts, and more.

---

## Table of Contents

- [Headers](#headers)
- [Query Parameters](#query-parameters)
- [Path Parameters](#path-parameters)
- [Cookies](#cookies)
- [Timeouts](#timeouts)
- [Authentication](#authentication)
- [Chaining and Immutability](#chaining-and-immutability)
- [Best Practices](#best-practices)

---

## Headers

### Single Header

```csharp
var response = await api.For("/endpoint")
    .WithHeader("Accept", "application/json")
    .Get()
    .ShouldReturn<Data>();
```

### Multiple Headers

```csharp
var response = await api.For("/endpoint")
    .WithHeaders(new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["User-Agent"] = "MyApp/1.0",
        ["X-API-Key"] = "your-api-key"
    })
    .Get()
    .ShouldReturn<Data>();
```

### Anonymous Object Headers

```csharp
var response = await api.For("/endpoint")
    .WithHeaders(new
    {
        Accept = "application/json",
        UserAgent = "MyApp/1.0",
        XApiKey = "your-api-key"
    })
    .Get()
    .ShouldReturn<Data>();
```

### Common Header Patterns

```csharp
// Content-Type for POST/PUT requests
var response = await api.For("/users")
    .WithHeader("Content-Type", "application/json")
    .Post(userData)
    .ShouldReturn<User>();

// Custom API headers
var response = await api.For("/data")
    .WithHeaders(new Dictionary<string, string>
    {
        ["X-Request-ID"] = Guid.NewGuid().ToString(),
        ["X-Timestamp"] = DateTime.UtcNow.ToString("O"),
        ["X-Version"] = "v1"
    })
    .Get()
    .ShouldReturn<Data>();

// Conditional headers
var headers = new Dictionary<string, string>
{
    ["Accept"] = "application/json"
};

if (includeDebugInfo)
{
    headers["X-Debug"] = "true";
}

var response = await api.For("/endpoint")
    .WithHeaders(headers)
    .Get()
    .ShouldReturn<Data>();
```

---

## Query Parameters

### Single Query Parameter

```csharp
var users = await api.For("/users")
    .WithQueryParam("active", true)
    .Get()
    .ShouldReturn<List<User>>();
```

### Multiple Query Parameters

```csharp
var users = await api.For("/users")
    .WithQueryParams(new Dictionary<string, object>
    {
        ["page"] = 1,
        ["limit"] = 10,
        ["sort"] = "name",
        ["order"] = "asc"
    })
    .Get()
    .ShouldReturn<List<User>>();
```

### Anonymous Object Parameters

```csharp
var users = await api.For("/users")
    .WithQueryParams(new
    {
        page = 1,
        limit = 10,
        sort = "name",
        order = "asc"
    })
    .Get()
    .ShouldReturn<List<User>>();
```

### Complex Query Parameters

```csharp
// Array parameters
var posts = await api.For("/posts")
    .WithQueryParam("tags", new[] { "csharp", "api", "testing" })
    .Get()
    .ShouldReturn<List<Post>>();

// Date parameters
var events = await api.For("/events")
    .WithQueryParams(new
    {
        startDate = DateTime.UtcNow.AddDays(-30),
        endDate = DateTime.UtcNow,
        category = "conference"
    })
    .Get()
    .ShouldReturn<List<Event>>();

// Null handling
var searchResults = await api.For("/search")
    .WithQueryParams(new
    {
        q = searchTerm,
        category = category, // null values are ignored
        limit = 20
    })
    .Get()
    .ShouldReturn<SearchResults>();
```

### URL Encoding

NaturalApi automatically handles URL encoding for query parameters:

```csharp
// Special characters are automatically encoded
var results = await api.For("/search")
    .WithQueryParam("q", "hello world & more")
    .Get()
    .ShouldReturn<SearchResults>();
// Results in: /search?q=hello%20world%20%26%20more
```

---

## Path Parameters

### Single Path Parameter

```csharp
var user = await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Get()
    .ShouldReturn<User>();
```

### Multiple Path Parameters

```csharp
var post = await api.For("/users/{userId}/posts/{postId}")
    .WithPathParams(new Dictionary<string, object>
    {
        ["userId"] = 123,
        ["postId"] = 456
    })
    .Get()
    .ShouldReturn<Post>();
```

### Anonymous Object Path Parameters

```csharp
var post = await api.For("/users/{userId}/posts/{postId}")
    .WithPathParams(new
    {
        userId = 123,
        postId = 456
    })
    .Get()
    .ShouldReturn<Post>();
```

### Complex Path Parameters

```csharp
// String parameters
var user = await api.For("/users/{username}")
    .WithPathParam("username", "john.doe")
    .Get()
    .ShouldReturn<User>();

// GUID parameters
var order = await api.For("/orders/{orderId}")
    .WithPathParam("orderId", Guid.NewGuid())
    .Get()
    .ShouldReturn<Order>();

// Multiple parameters with different types
var resource = await api.For("/tenants/{tenantId}/resources/{resourceId}")
    .WithPathParams(new
    {
        tenantId = "tenant-123",
        resourceId = 456
    })
    .Get()
    .ShouldReturn<Resource>();
```

---

## Cookies

### Single Cookie

```csharp
var response = await api.For("/protected")
    .WithCookie("session", "abc123")
    .Get()
    .ShouldReturn<Data>();
```

### Multiple Cookies

```csharp
var response = await api.For("/dashboard")
    .WithCookies(new Dictionary<string, string>
    {
        ["session"] = "abc123",
        ["theme"] = "dark",
        ["language"] = "en"
    })
    .Get()
    .ShouldReturn<DashboardData>();
```

### Anonymous Object Cookies

```csharp
var response = await api.For("/dashboard")
    .WithCookies(new
    {
        session = "abc123",
        theme = "dark",
        language = "en"
    })
    .Get()
    .ShouldReturn<DashboardData>();
```

### Cookie Management

```csharp
// Clear all cookies
var response = await api.For("/logout")
    .ClearCookies()
    .Post()
    .ShouldReturn<LogoutResult>();

// Session management
var loginResponse = await api.For("/login")
    .Post(credentials)
    .ShouldReturn<LoginResult>();

// Use session cookie for subsequent requests
var userData = await api.For("/user/profile")
    .WithCookie("session", loginResponse.SessionToken)
    .Get()
    .ShouldReturn<UserProfile>();
```

### Cookie with Attributes

```csharp
// Note: NaturalApi handles basic cookie values
// For complex cookie attributes (domain, path, etc.), you may need to set them via headers
var response = await api.For("/endpoint")
    .WithHeader("Cookie", "session=abc123; domain=.example.com; path=/")
    .Get()
    .ShouldReturn<Data>();
```

---

## Timeouts

### Per-Request Timeout

```csharp
var data = await api.For("/slow-endpoint")
    .WithTimeout(TimeSpan.FromMinutes(5))
    .Get()
    .ShouldReturn<Data>();
```

### Different Timeout Scenarios

```csharp
// Quick timeout for health checks
var health = await api.For("/health")
    .WithTimeout(TimeSpan.FromSeconds(5))
    .Get()
    .ShouldReturn<HealthStatus>();

// Longer timeout for file uploads
var upload = await api.For("/upload")
    .WithTimeout(TimeSpan.FromMinutes(10))
    .Post(fileData)
    .ShouldReturn<UploadResult>();

// Very short timeout for testing
var quickTest = await api.For("/test")
    .WithTimeout(TimeSpan.FromMilliseconds(100))
    .Get()
    .ShouldReturn<TestResult>();
```

### Timeout Handling

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
    Console.WriteLine("Request timed out");
    // Handle timeout scenario
}
```

---

## Authentication

### Inline Authentication

```csharp
// Bearer token
var data = await api.For("/protected")
    .UsingAuth("Bearer your-token")
    .Get()
    .ShouldReturn<Data>();

// Simple token (automatically adds Bearer)
var data = await api.For("/protected")
    .UsingAuth("your-token")
    .Get()
    .ShouldReturn<Data>();

// UsingToken shortcut
var data = await api.For("/protected")
    .UsingToken("your-token")
    .Get()
    .ShouldReturn<Data>();
```

> **🔐 Authentication Guide:** For advanced authentication patterns, auth providers, and caching, see the [Authentication Guide](authentication.md).

### Per-User Authentication

```csharp
// Authenticate as specific user
var userData = await api.For("/users/me")
    .AsUser("john.doe")
    .Get()
    .ShouldReturn<UserData>();
```

### Disabling Authentication

```csharp
// Override default auth for this request
var publicData = await api.For("/public")
    .WithoutAuth()
    .Get()
    .ShouldReturn<PublicData>();
```

---

## Chaining and Immutability

### Method Chaining

All request building methods return new `IApiContext` instances, allowing for fluent chaining:

```csharp
var response = await api.For("/complex-endpoint")
    .WithHeader("Accept", "application/json")
    .WithHeader("User-Agent", "MyApp/1.0")
    .WithQueryParam("page", 1)
    .WithQueryParam("limit", 10)
    .WithPathParam("id", 123)
    .WithCookie("session", "abc123")
    .WithTimeout(TimeSpan.FromSeconds(30))
    .UsingAuth("Bearer token")
    .Get()
    .ShouldReturn<Data>();
```

### Immutability

Each method call creates a new context, so you can safely reuse base contexts:

```csharp
// Create base context
var baseContext = api.For("/users")
    .WithHeader("Accept", "application/json")
    .WithQueryParam("active", true);

// Use for different operations
var activeUsers = await baseContext
    .Get()
    .ShouldReturn<List<User>>();

var newUser = await baseContext
    .Post(userData)
    .ShouldReturn<User>(status: 201);

// Original context is unchanged
var allUsers = await baseContext
    .WithQueryParam("active", null) // Override previous parameter
    .Get()
    .ShouldReturn<List<User>>();
```

### Conditional Building

```csharp
public async Task<Data> GetData(bool includeMetadata, string? filter = null)
{
    var context = api.For("/data")
        .WithHeader("Accept", "application/json");

    if (includeMetadata)
    {
        context = context.WithQueryParam("include", "metadata");
    }

    if (!string.IsNullOrEmpty(filter))
    {
        context = context.WithQueryParam("filter", filter);
    }

    return await context.Get().ShouldReturn<Data>();
}
```

---

## Best Practices

### 1. Use Meaningful Parameter Names

```csharp
// Good
.WithQueryParam("userId", 123)
.WithPathParam("orderId", orderId)

// Avoid
.WithQueryParam("id", 123)  // Unclear what type of ID
```

### 2. Group Related Parameters

```csharp
// Good - group related parameters
var searchParams = new Dictionary<string, object>
{
    ["q"] = searchTerm,
    ["category"] = category,
    ["sort"] = sortBy,
    ["order"] = sortOrder
};

var results = await api.For("/search")
    .WithQueryParams(searchParams)
    .Get()
    .ShouldReturn<SearchResults>();
```

### 3. Handle Null Values Appropriately

```csharp
// Filter out null values
var parameters = new Dictionary<string, object>();
if (!string.IsNullOrEmpty(searchTerm))
    parameters["q"] = searchTerm;
if (category != null)
    parameters["category"] = category;
if (dateRange != null)
    parameters["dateRange"] = dateRange;

var results = await api.For("/search")
    .WithQueryParams(parameters)
    .Get()
    .ShouldReturn<SearchResults>();
```

### 4. Use Consistent Header Patterns

```csharp
public class ApiClient
{
    private readonly IApi _api;
    private readonly string _userAgent;

    public ApiClient(IApi api, string userAgent)
    {
        _api = api;
        _userAgent = userAgent;
    }

    private IApiContext CreateBaseContext(string endpoint)
    {
        return _api.For(endpoint)
            .WithHeader("Accept", "application/json")
            .WithHeader("User-Agent", _userAgent);
    }

    public async Task<List<User>> GetUsers()
    {
        return await CreateBaseContext("/users")
            .Get()
            .ShouldReturn<List<User>>();
    }
}
```

### 5. Handle Special Characters

```csharp
// NaturalApi handles URL encoding automatically
var results = await api.For("/search")
    .WithQueryParam("q", "hello world & special chars!")
    .Get()
    .ShouldReturn<SearchResults>();
// Results in: /search?q=hello%20world%20%26%20special%20chars%21
```

### 6. Use Type-Safe Parameters

```csharp
// Use strongly-typed objects when possible
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
}

var searchRequest = new SearchRequest
{
    Query = "natural api",
    Category = "documentation",
    Page = 1,
    Limit = 20
};

var results = await api.For("/search")
    .WithQueryParams(searchRequest)
    .Get()
    .ShouldReturn<SearchResults>();
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic request building concepts
- **[HTTP Verbs](http-verbs.md)** - Making GET, POST, PUT, PATCH, DELETE requests
- **[Assertions](assertions.md)** - Validating responses
- **[Authentication](authentication.md)** - Advanced authentication patterns
- **[Configuration](configuration.md)** - Setting up default headers and timeouts
- **[Examples](examples.md)** - Real-world request building scenarios
- **[Troubleshooting](troubleshooting.md)** - Common request building issues
- **[Fluent Syntax Reference](fluentsyntax.md)** - Complete method reference
- **[API Reference](api-reference.md)** - Complete interface documentation
