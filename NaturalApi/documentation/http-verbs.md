# HTTP Verbs Guide

> NaturalApi supports all standard HTTP methods with a consistent, fluent interface. This guide covers GET, POST, PUT, PATCH, and DELETE with detailed examples and best practices.

---

## Table of Contents

- [GET Requests](#get-requests)
- [POST Requests](#post-requests)
- [PUT Requests](#put-requests)
- [PATCH Requests](#patch-requests)
- [DELETE Requests](#delete-requests)
- [Request Bodies](#request-bodies)
- [Status Code Expectations](#status-code-expectations)
- [Best Practices](#best-practices)

---

## GET Requests

GET requests are used to retrieve data from the server. They should be idempotent and have no side effects.

### Basic GET Request

```csharp
// Simple GET request
var users = await api.For("/users")
    .Get()
    .ShouldReturn<List<User>>();
```

### GET with Query Parameters

```csharp
// Single query parameter
var users = await api.For("/users")
    .WithQueryParam("active", true)
    .Get()
    .ShouldReturn<List<User>>();

// Multiple query parameters
var posts = await api.For("/posts")
    .WithQueryParams(new
    {
        page = 1,
        limit = 10,
        sort = "created_at",
        order = "desc"
    })
    .Get()
    .ShouldReturn<List<Post>>();
```

### GET with Path Parameters

```csharp
// Single path parameter
var user = await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Get()
    .ShouldReturn<User>();

// Multiple path parameters
var post = await api.For("/users/{userId}/posts/{postId}")
    .WithPathParams(new
    {
        userId = 123,
        postId = 456
    })
    .Get()
    .ShouldReturn<Post>();
```

### GET with Headers

```csharp
var data = await api.For("/protected-data")
    .WithHeaders(new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["Authorization"] = "Bearer your-token"
    })
    .Get()
    .ShouldReturn<Data>();
```

### GET with Authentication

```csharp
// Using inline auth
var profile = await api.For("/users/me")
    .UsingAuth("Bearer your-token")
    .Get()
    .ShouldReturn<UserProfile>();

// Using auth provider (if configured)
var profile = await api.For("/users/me")
    .Get()  // Auth is automatic
    .ShouldReturn<UserProfile>();
```

### Complex GET Examples

```csharp
// Search with filters
var results = await api.For("/search")
    .WithQueryParams(new
    {
        q = "natural api",
        category = "documentation",
        dateFrom = DateTime.UtcNow.AddDays(-30),
        dateTo = DateTime.UtcNow,
        page = 1,
        limit = 20
    })
    .Get()
    .ShouldReturn<SearchResults>();

// Nested resource with authentication
var comments = await api.For("/posts/{postId}/comments")
    .WithPathParam("postId", 123)
    .WithQueryParam("include", "author")
    .UsingAuth("Bearer token")
    .Get()
    .ShouldReturn<List<Comment>>();
```

---

## POST Requests

POST requests are used to create new resources or submit data to the server.

### Basic POST Request

```csharp
// Simple POST with object
var newUser = await api.For("/users")
    .Post(new
    {
        name = "John Doe",
        email = "john@example.com"
    })
    .ShouldReturn<User>(status: 201);
```

### POST with Headers

```csharp
var newPost = await api.For("/posts")
    .WithHeaders(new Dictionary<string, string>
    {
        ["Content-Type"] = "application/json"
    })
    .Post(new
    {
        title = "My Post",
        content = "This is the content",
        authorId = 123
    })
    .ShouldReturn<Post>(status: 201);
```

### POST with Authentication

```csharp
var newOrder = await api.For("/orders")
    .UsingAuth("Bearer token")
    .Post(new
    {
        items = new[] { 1, 2, 3 },
        shippingAddress = "123 Main St"
    })
    .ShouldReturn<Order>(status: 201);
```

### POST with Form Data

```csharp
// For form-encoded data
var response = await api.For("/contact")
    .WithHeader("Content-Type", "application/x-www-form-urlencoded")
    .Post(new
    {
        name = "John Doe",
        email = "john@example.com",
        message = "Hello world"
    })
    .ShouldReturn<ContactResponse>();
```

### POST with File Upload

```csharp
// File upload scenario
var uploadResult = await api.For("/upload")
    .WithHeaders(new Dictionary<string, string>
    {
        ["Content-Type"] = "multipart/form-data"
    })
    .Post(new
    {
        file = fileData,
        description = "My file upload"
    })
    .ShouldReturn<UploadResult>(status: 201);
```

### POST without Body

```csharp
// Some POST endpoints don't require a body
var result = await api.For("/process")
    .Post()  // No body parameter
    .ShouldReturn<ProcessResult>(status: 202);
```

---

## PUT Requests

PUT requests are used to update or replace entire resources. They should be idempotent.

### Basic PUT Request

```csharp
var updatedUser = await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Put(new
    {
        id = 123,
        name = "John Updated",
        email = "john.updated@example.com"
    })
    .ShouldReturn<User>(status: 200);
```

### PUT with Complete Resource

```csharp
var updatedPost = await api.For("/posts/{id}")
    .WithPathParam("id", 456)
    .WithHeaders(new Dictionary<string, string>
    {
        ["Content-Type"] = "application/json"
    })
    .Put(new
    {
        id = 456,
        title = "Updated Title",
        content = "Updated content",
        authorId = 123,
        publishedAt = DateTime.UtcNow
    })
    .ShouldReturn<Post>(status: 200);
```

### PUT with Authentication

```csharp
var updatedProfile = await api.For("/users/{id}/profile")
    .WithPathParam("id", 123)
    .UsingAuth("Bearer token")
    .Put(new
    {
        bio = "Updated bio",
        location = "New York",
        website = "https://johndoe.com"
    })
    .ShouldReturn<UserProfile>(status: 200);
```

---

## PATCH Requests

PATCH requests are used for partial updates to resources. Only the fields that need to be changed are included.

### Basic PATCH Request

```csharp
// Partial update - only update specific fields
var updatedUser = await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Patch(new
    {
        email = "newemail@example.com"  // Only update email
    })
    .ShouldReturn<User>(status: 200);
```

### PATCH with Multiple Fields

```csharp
var updatedPost = await api.For("/posts/{id}")
    .WithPathParam("id", 456)
    .Patch(new
    {
        title = "Updated Title",
        publishedAt = DateTime.UtcNow
        // content and authorId remain unchanged
    })
    .ShouldReturn<Post>(status: 200);
```

### PATCH with Authentication

```csharp
var updatedSettings = await api.For("/users/{id}/settings")
    .WithPathParam("id", 123)
    .UsingAuth("Bearer token")
    .Patch(new
    {
        notifications = true,
        theme = "dark"
    })
    .ShouldReturn<UserSettings>(status: 200);
```

### PATCH for Status Updates

```csharp
// Common pattern for status updates
var updatedOrder = await api.For("/orders/{id}")
    .WithPathParam("id", 789)
    .Patch(new
    {
        status = "shipped",
        trackingNumber = "TRK123456"
    })
    .ShouldReturn<Order>(status: 200);
```

---

## DELETE Requests

DELETE requests are used to remove resources from the server.

### Basic DELETE Request

```csharp
// Simple DELETE
await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Delete()
    .ShouldReturn(status: 204);
```

### DELETE with Authentication

```csharp
await api.For("/posts/{id}")
    .WithPathParam("id", 456)
    .UsingAuth("Bearer token")
    .Delete()
    .ShouldReturn(status: 204);
```

### DELETE with Query Parameters

```csharp
// DELETE with additional parameters
await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .WithQueryParam("cascade", true)
    .Delete()
    .ShouldReturn(status: 204);
```

### DELETE with Confirmation

```csharp
// Some APIs require confirmation for DELETE
await api.For("/dangerous-resource/{id}")
    .WithPathParam("id", 789)
    .WithQueryParam("confirm", true)
    .Delete()
    .ShouldReturn(status: 204);
```

---

## Request Bodies

### Anonymous Objects

```csharp
// Simple anonymous object
var result = await api.For("/endpoint")
    .Post(new
    {
        name = "John",
        age = 30,
        active = true
    })
    .ShouldReturn<Result>();
```

### Strongly-Typed Objects

```csharp
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

var request = new CreateUserRequest
{
    Name = "John Doe",
    Email = "john@example.com",
    Age = 30
};

var user = await api.For("/users")
    .Post(request)
    .ShouldReturn<User>(status: 201);
```

### Collections and Arrays

```csharp
// Array of items
var result = await api.For("/items")
    .Post(new
    {
        items = new[] { "item1", "item2", "item3" },
        category = "test"
    })
    .ShouldReturn<Result>();

// List of objects
var orders = await api.For("/orders/bulk")
    .Post(new
    {
        orders = new[]
        {
            new { productId = 1, quantity = 2 },
            new { productId = 2, quantity = 1 }
        }
    })
    .ShouldReturn<BulkOrderResult>();
```

### Null and Optional Bodies

```csharp
// POST without body
var result = await api.For("/trigger")
    .Post()  // No body
    .ShouldReturn<TriggerResult>();

// POST with null body
var result = await api.For("/endpoint")
    .Post(null)  // Explicit null
    .ShouldReturn<Result>();
```

---

## Status Code Expectations

### Common Status Codes

```csharp
// 200 OK - Successful GET, PUT, PATCH
var data = await api.For("/data")
    .Get()
    .ShouldReturn<Data>(status: 200);

// 201 Created - Successful POST
var user = await api.For("/users")
    .Post(newUser)
    .ShouldReturn<User>(status: 201);

// 204 No Content - Successful DELETE
await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Delete()
    .ShouldReturn(status: 204);

// 202 Accepted - Async processing
var job = await api.For("/process")
    .Post(processData)
    .ShouldReturn<Job>(status: 202);
```

### Status Code Validation

```csharp
// Validate status code only
await api.For("/endpoint")
    .Get()
    .ShouldReturn(status: 200);

// Validate status code and type
var data = await api.For("/endpoint")
    .Get()
    .ShouldReturn<Data>(status: 200);

// Validate status code and body
var result = await api.For("/endpoint")
    .Post(payload)
    .ShouldReturn<Result>(
        status: 201,
        body: r => r.Id > 0 && !string.IsNullOrEmpty(r.Name)
    );
```

---

## Best Practices

### 1. Use Appropriate HTTP Methods

```csharp
// GET for retrieving data
var users = await api.For("/users").Get().ShouldReturn<List<User>>();

// POST for creating new resources
var user = await api.For("/users").Post(newUser).ShouldReturn<User>(status: 201);

// PUT for complete updates
var user = await api.For("/users/{id}").WithPathParam("id", 123).Put(updatedUser).ShouldReturn<User>();

// PATCH for partial updates
var user = await api.For("/users/{id}").WithPathParam("id", 123).Patch(partialUpdate).ShouldReturn<User>();

// DELETE for removing resources
await api.For("/users/{id}").WithPathParam("id", 123).Delete().ShouldReturn(status: 204);
```

### 2. Handle Different Response Types

```csharp
// Single resource
var user = await api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Get()
    .ShouldReturn<User>();

// Collection of resources
var users = await api.For("/users")
    .Get()
    .ShouldReturn<List<User>>();

// Paginated results
var page = await api.For("/users")
    .WithQueryParam("page", 1)
    .Get()
    .ShouldReturn<PagedResult<User>>();
```

### 3. Use Consistent Error Handling

```csharp
try
{
    var user = await api.For("/users/{id}")
        .WithPathParam("id", 999)
        .Get()
        .ShouldReturn<User>();
}
catch (ApiAssertionException ex)
{
    // Handle 404 Not Found
    Console.WriteLine($"User not found: {ex.Message}");
}
catch (ApiExecutionException ex)
{
    // Handle network or server errors
    Console.WriteLine($"Request failed: {ex.Message}");
}
```

### 4. Chain Related Operations

```csharp
// Create user and then get their profile
var newUser = await api.For("/users")
    .Post(userData)
    .ShouldReturn<User>(status: 201);

var profile = await api.For("/users/{id}/profile")
    .WithPathParam("id", newUser.Id)
    .Get()
    .ShouldReturn<UserProfile>();
```

### 5. Use Type-Safe Request Objects

```csharp
public class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public string ShippingAddress { get; set; } = string.Empty;
}

var request = new CreateOrderRequest
{
    CustomerId = 123,
    Items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 2 },
        new() { ProductId = 2, Quantity = 1 }
    },
    ShippingAddress = "123 Main St"
};

var order = await api.For("/orders")
    .Post(request)
    .ShouldReturn<Order>(status: 201);
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic HTTP verb usage
- **[Request Building](request-building.md)** - Headers, query params, path params, cookies
- **[Assertions](assertions.md)** - Validating responses and status codes
- **[Authentication](authentication.md)** - Adding authentication to requests
- **[Examples](examples.md)** - Real-world HTTP verb usage scenarios
- **[Error Handling](error-handling.md)** - Handling HTTP errors and exceptions
- **[Fluent Syntax Reference](fluentsyntax.md)** - Complete method reference
- **[API Reference](api-reference.md)** - Complete interface documentation
