# Assertions Guide

> NaturalApi's `ShouldReturn` method provides comprehensive assertion capabilities for validating HTTP responses. This guide covers all assertion patterns, from simple type validation to complex multi-criteria validation.

---

## Table of Contents

- [Basic Assertions](#basic-assertions)
- [Type Validation](#type-validation)
- [Status Code Validation](#status-code-validation)
- [Body Validation](#body-validation)
- [Header Validation](#header-validation)
- [Combined Assertions](#combined-assertions)
- [Manual Response Access](#manual-response-access)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

---

## Basic Assertions

### Type-Only Validation

```csharp
// Just validate that the response can be deserialized to the expected type
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>();
```

### Status Code-Only Validation

```csharp
// Just validate the HTTP status code
await api.For("/users/1")
    .Delete()
    .ShouldReturn(status: 204);
```

### Type and Status Validation

```csharp
// Validate both type and status code
var user = await api.For("/users")
    .Post(newUser)
    .ShouldReturn<User>(status: 201);
```

---

## Type Validation

### Simple Type Validation

```csharp
// Single object
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>();

// Collection
var users = await api.For("/users")
    .Get()
    .ShouldReturn<List<User>>();

// Generic collection
var posts = await api.For("/posts")
    .Get()
    .ShouldReturn<IEnumerable<Post>>();
```

### Complex Type Validation

```csharp
// Nested objects
var order = await api.For("/orders/123")
    .Get()
    .ShouldReturn<Order>();

// Generic types
var result = await api.For("/search")
    .Get()
    .ShouldReturn<SearchResult<Post>>();

// Anonymous types (for simple responses)
var response = await api.For("/status")
    .Get()
    .ShouldReturn<object>();
```

### Type Validation with Deserialization

```csharp
// NaturalApi automatically handles JSON deserialization
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>();

// Access properties after validation
Console.WriteLine($"User: {user.Name}, Email: {user.Email}");
```

---

## Status Code Validation

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
await api.For("/users/123")
    .Delete()
    .ShouldReturn(status: 204);

// 202 Accepted - Async processing
var job = await api.For("/process")
    .Post(processData)
    .ShouldReturn<Job>(status: 202);
```

### Status Code Ranges

```csharp
// Validate success status codes (200-299)
var data = await api.For("/data")
    .Get()
    .ShouldReturn<Data>(status: 200);

// Validate client error (400-499)
try
{
    await api.For("/invalid")
        .Get()
        .ShouldReturn(status: 200);
}
catch (ApiAssertionException ex)
{
    // Expected 400 Bad Request
    Assert.IsTrue(ex.Message.Contains("400"));
}
```

---

## Body Validation

### Simple Property Validation

```csharp
// Validate specific properties
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(body: u => u.Id == 1);

// Multiple property validation
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(body: u => 
        u.Id == 1 && 
        !string.IsNullOrEmpty(u.Name) && 
        u.Email.Contains("@")
    );
```

### Complex Object Validation

```csharp
// Nested object validation
var order = await api.For("/orders/123")
    .Get()
    .ShouldReturn<Order>(body: o => 
        o.Id == 123 && 
        o.Items.Count > 0 && 
        o.Total > 0 &&
        o.Customer.Id > 0
    );

// Collection validation
var posts = await api.For("/posts")
    .Get()
    .ShouldReturn<List<Post>>(body: posts => 
        posts.Count > 0 && 
        posts.All(p => p.Id > 0) &&
        posts.All(p => !string.IsNullOrEmpty(p.Title))
    );
```

### Business Logic Validation

```csharp
// Validate business rules
var order = await api.For("/orders/123")
    .Get()
    .ShouldReturn<Order>(body: o => 
        o.Status == OrderStatus.Confirmed &&
        o.Items.Sum(i => i.Price * i.Quantity) == o.Total &&
        o.ShippingAddress != null
    );

// Date validation
var event = await api.For("/events/456")
    .Get()
    .ShouldReturn<Event>(body: e => 
        e.StartDate > DateTime.UtcNow &&
        e.EndDate > e.StartDate
    );
```

### String Validation

```csharp
// String content validation
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(body: u => 
        u.Name.Length > 0 &&
        u.Email.Contains("@") &&
        u.Email.Contains(".")
    );

// Pattern validation
var product = await api.For("/products/123")
    .Get()
    .ShouldReturn<Product>(body: p => 
        !string.IsNullOrEmpty(p.Sku) &&
        p.Sku.Length >= 5 &&
        p.Price > 0
    );
```

---

## Header Validation

### Single Header Validation

```csharp
// Validate specific header
var response = await api.For("/data")
    .Get()
    .ShouldReturn<Data>(headers: h => 
        h.ContainsKey("Content-Type") &&
        h["Content-Type"].Contains("application/json")
    );
```

### Multiple Header Validation

```csharp
// Validate multiple headers
var response = await api.For("/protected")
    .Get()
    .ShouldReturn<Data>(headers: h => 
        h.ContainsKey("Content-Type") &&
        h.ContainsKey("Cache-Control") &&
        h["Content-Type"].Contains("application/json") &&
        h["Cache-Control"].Contains("no-cache")
    );
```

### Header Value Validation

```csharp
// Validate header values
var response = await api.For("/data")
    .Get()
    .ShouldReturn<Data>(headers: h => 
        h.ContainsKey("X-Request-ID") &&
        Guid.TryParse(h["X-Request-ID"], out _) &&
        h.ContainsKey("X-Response-Time") &&
        int.TryParse(h["X-Response-Time"], out var time) &&
        time < 1000
    );
```

### Custom Header Validation

```csharp
// Validate custom headers
var response = await api.For("/api/data")
    .Get()
    .ShouldReturn<Data>(headers: h => 
        h.ContainsKey("X-API-Version") &&
        h["X-API-Version"] == "v1" &&
        h.ContainsKey("X-Rate-Limit-Remaining") &&
        int.Parse(h["X-Rate-Limit-Remaining"]) > 0
    );
```

---

## Combined Assertions

### Type, Status, and Body Validation

```csharp
// Complete validation
var user = await api.For("/users")
    .Post(newUser)
    .ShouldReturn<User>(
        status: 201,
        body: u => 
            u.Id > 0 && 
            !string.IsNullOrEmpty(u.Name) &&
            u.Email.Contains("@"),
        headers: h => 
            h.ContainsKey("Location") &&
            h["Location"].Contains("/users/")
    );
```

### Complex Multi-Criteria Validation

```csharp
// Validate order creation
var order = await api.For("/orders")
    .Post(orderData)
    .ShouldReturn<Order>(
        status: 201,
        body: o => 
            o.Id > 0 &&
            o.Status == OrderStatus.Pending &&
            o.Items.Count > 0 &&
            o.Total > 0 &&
            o.Customer.Id > 0,
        headers: h => 
            h.ContainsKey("Location") &&
            h.ContainsKey("X-Order-ID") &&
            h["X-Order-ID"] == o.Id.ToString()
    );
```

### Pagination Validation

```csharp
// Validate paginated response
var page = await api.For("/users")
    .WithQueryParam("page", 1)
    .WithQueryParam("limit", 10)
    .Get()
    .ShouldReturn<PagedResult<User>>(
        status: 200,
        body: p => 
            p.Items.Count <= 10 &&
            p.Page == 1 &&
            p.TotalCount > 0 &&
            p.Items.All(u => u.Id > 0)
    );
```

---

## Manual Response Access

### Accessing Response Properties

```csharp
// Get the result context for manual inspection
var result = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>();

// Access response properties
Console.WriteLine($"Status: {result.StatusCode}");
Console.WriteLine($"Headers: {string.Join(", ", result.Headers)}");
Console.WriteLine($"Raw Body: {result.RawBody}");

// Access the underlying HttpResponseMessage
var response = result.Response;
Console.WriteLine($"Content Type: {response.Content.Headers.ContentType}");
```

### Manual Deserialization

```csharp
// Manual deserialization when needed
var result = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>();

// Access deserialized body
var user = result.BodyAs<User>();
Console.WriteLine($"User: {user.Name}");

// Access raw body for custom processing
var rawBody = result.RawBody;
var customObject = JsonSerializer.Deserialize<CustomType>(rawBody);
```

### Response Headers Access

```csharp
// Access specific headers
var result = await api.For("/data")
    .Get()
    .ShouldReturn<Data>();

// Get specific header values
var contentType = result.Headers.GetValueOrDefault("Content-Type");
var requestId = result.Headers.GetValueOrDefault("X-Request-ID");

// Check for header existence
if (result.Headers.ContainsKey("Cache-Control"))
{
    var cacheControl = result.Headers["Cache-Control"];
    // Process cache control directive
}
```

---

## Error Handling

### Exception Types

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
    // Message: "Expected status 200 but got 404 for GET /users/999"
}
catch (ApiExecutionException ex)
{
    // Network error, timeout, or HTTP error
    Console.WriteLine($"Request failed: {ex.Message}");
}
```

### Debugging Failed Assertions

```csharp
try
{
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>(body: u => u.Name == "Expected Name");
}
catch (ApiAssertionException ex)
{
    // Get detailed error information
    Console.WriteLine($"Expected: {ex.Message}");
    Console.WriteLine($"Actual status: {ex.ActualStatusCode}");
    Console.WriteLine($"Response body: {ex.ResponseBody}");
}
```

### Graceful Error Handling

```csharp
// Handle different error scenarios
try
{
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>(status: 200);
}
catch (ApiAssertionException ex) when (ex.ActualStatusCode == 404)
{
    // Handle 404 Not Found
    Console.WriteLine("User not found");
}
catch (ApiAssertionException ex) when (ex.ActualStatusCode == 401)
{
    // Handle 401 Unauthorized
    Console.WriteLine("Authentication required");
}
catch (ApiExecutionException ex)
{
    // Handle network errors
    Console.WriteLine($"Network error: {ex.Message}");
}
```

---

## Best Practices

### 1. Use Specific Assertions

```csharp
// Good - specific validation
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(body: u => u.Id == 1 && u.Name == "John");

// Avoid - too generic
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(); // No validation
```

### 2. Validate Business Rules

```csharp
// Validate business logic, not just data structure
var order = await api.For("/orders/123")
    .Get()
    .ShouldReturn<Order>(body: o => 
        o.Status == OrderStatus.Confirmed &&
        o.Items.All(i => i.Quantity > 0) &&
        o.Total == o.Items.Sum(i => i.Price * i.Quantity)
    );
```

### 3. Use Meaningful Error Messages

```csharp
// Custom validation with clear error messages
var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(body: u => 
    {
        if (u.Id != 1) throw new ArgumentException($"Expected user ID 1, got {u.Id}");
        if (string.IsNullOrEmpty(u.Name)) throw new ArgumentException("User name is required");
        return true;
    });
```

### 4. Handle Edge Cases

```csharp
// Validate edge cases
var users = await api.For("/users")
    .Get()
    .ShouldReturn<List<User>>(body: users => 
        users.Count >= 0 && // Handle empty list
        users.All(u => u.Id > 0) && // All users have valid IDs
        users.All(u => !string.IsNullOrEmpty(u.Name)) // All users have names
    );
```

### 5. Use Type-Safe Validation

```csharp
// Use strongly-typed validation
public class UserValidator
{
    public static bool IsValid(User user)
    {
        return user.Id > 0 &&
               !string.IsNullOrEmpty(user.Name) &&
               user.Email.Contains("@") &&
               user.CreatedAt <= DateTime.UtcNow;
    }
}

var user = await api.For("/users/1")
    .Get()
    .ShouldReturn<User>(body: UserValidator.IsValid);
```

### 6. Validate Response Headers

```csharp
// Always validate important headers
var response = await api.For("/api/data")
    .Get()
    .ShouldReturn<Data>(headers: h => 
        h.ContainsKey("Content-Type") &&
        h["Content-Type"].Contains("application/json") &&
        h.ContainsKey("X-API-Version")
    );
```

### 7. Use Descriptive Test Names

```csharp
[TestMethod]
public async Task Get_User_Should_Return_Valid_User_With_Correct_Properties()
{
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>(body: u => 
            u.Id == 1 && 
            !string.IsNullOrEmpty(u.Name) &&
            u.Email.Contains("@")
        );
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic assertion concepts
- **[HTTP Verbs](http-verbs.md)** - Making requests with different HTTP methods
- **[Error Handling](error-handling.md)** - Handling exceptions and errors
- **[Testing Guide](testing-guide.md)** - Testing patterns and best practices
- **[Examples](examples.md)** - Real-world assertion scenarios
- **[Troubleshooting](troubleshooting.md)** - Common assertion issues
- **[Fluent Syntax Reference](fluentsyntax.md)** - Complete method reference
- **[API Reference](api-reference.md)** - Complete interface documentation
