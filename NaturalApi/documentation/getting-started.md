# Getting Started with NaturalApi

> **NaturalApi** makes API testing as natural as describing it aloud. This guide will get you up and running in minutes.

---

## Prerequisites

- .NET 9.0 or later
- Visual Studio 2022, VS Code, or your preferred .NET IDE
- Basic familiarity with C# and HTTP APIs

---

## Installation

Add NaturalApi to your project using the .NET CLI:

```bash
dotnet add package NaturalApi
```

Or via Package Manager in Visual Studio:

```
Install-Package NaturalApi
```

Or add to your `.csproj` file:

```xml
<PackageReference Include="NaturalApi" Version="1.0.0" />
```

---

## Your First API Call

Let's start with the simplest possible example:

```csharp
using NaturalApi;

// Ultra-simple: No base URL needed, just use absolute URLs
var api = new Api();

// Make a GET request with absolute URL
var users = await api.For("https://jsonplaceholder.typicode.com/users")
    .Get()
    .ShouldReturn<List<User>>();

Console.WriteLine($"Found {users.Count} users");
```

That's it! No HttpClient setup, no JSON deserialization boilerplate, no status code checking. NaturalApi handles it all.

### With Base URL (Even Simpler)

If you're calling the same API repeatedly, set a base URL:

```csharp
// With base URL - now you can use relative endpoints
var api = new Api("https://jsonplaceholder.typicode.com");

// Much cleaner with relative URLs
var users = await api.For("/users")
    .Get()
    .ShouldReturn<List<User>>();
```

---

## Understanding the Flow

Every NaturalApi call follows this natural pattern:

```
Api.For(endpoint)           // 1. Where are we going?
   .WithHeaders(...)         // 2. What headers do we need? (optional)
   .WithQueryParams(...)     // 3. What parameters? (optional)
   .UsingAuth(...)          // 4. How do we authenticate? (optional)
   .Get()                    // 5. What HTTP method?
   .ShouldReturn<T>()       // 6. What do we expect back?
```

You can read this aloud: *"For this endpoint, with these headers, using this auth, get the data, and it should return a list of users."*

> **📚 Learn More:** See the complete [Fluent Syntax Reference](fluentsyntax.md) for all available methods and the [Philosophy & Design Principles](philosophyanddesignprinciples.md) for the reasoning behind this design.

---

## Creating Your First Test

Let's create a proper test using MSTest:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

[TestClass]
public class UserApiTests
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _api = new Api("https://jsonplaceholder.typicode.com");
    }

    [TestMethod]
    public async Task Get_Users_Should_Return_User_List()
    {
        // Act & Assert
        var users = await _api.For("/users")
            .Get()
            .ShouldReturn<List<User>>();

        // Verify
        Assert.IsNotNull(users);
        Assert.IsTrue(users.Count > 0);
        Assert.IsNotNull(users.First().Name);
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

Run this test and watch it pass. No mocking, no setup - just a real API call with automatic validation.

### No Dependency Injection? No Problem!

NaturalApi works perfectly without DI. Just create instances directly:

```csharp
[TestMethod]
public async Task Ultra_Simple_Test_Without_DI()
{
    // No DI needed - just create the API directly
    var api = new Api("https://jsonplaceholder.typicode.com");
    
    var users = await api.For("/users")
        .Get()
        .ShouldReturn<List<User>>();
        
    Assert.IsTrue(users.Count > 0);
}
```

This is perfect for simple tests, scripts, or when you don't want to set up dependency injection.

---

## Making POST Requests

Creating resources is just as natural:

```csharp
[TestMethod]
public async Task Create_User_Should_Return_New_User()
{
    // Arrange
    var newUser = new
    {
        name = "John Doe",
        email = "john@example.com",
        username = "johndoe"
    };

    // Act & Assert
    var createdUser = await _api.For("/users")
        .WithHeaders(new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        })
        .Post(newUser)
        .ShouldReturn<User>(status: 201);

    // Verify
    Assert.IsNotNull(createdUser);
    Assert.AreEqual("John Doe", createdUser.Name);
    Assert.AreEqual("john@example.com", createdUser.Email);
}
```

Notice how we:
1. Define the endpoint (`/users`)
2. Set the content type header
3. POST the data
4. Expect a 201 status code and a User object back

All in one fluent chain.

---

## Adding Authentication

Many APIs require authentication. NaturalApi makes this incredibly simple with the new `AsUser()` method:

```csharp
[TestMethod]
public async Task Get_Protected_Resource_With_Credentials()
{
    // Just provide username and password - NaturalApi handles the rest!
    var data = await _api.For("/protected/data")
        .AsUser("myusername", "mypassword")
        .Get()
        .ShouldReturn<ProtectedData>();

    Assert.IsNotNull(data);
}
```

The `AsUser(username, password)` method automatically:
1. Calls your authentication service
2. Gets a token
3. Adds the `Authorization: Bearer <token>` header
4. Handles token caching

### Traditional Token Authentication

You can still use tokens directly:

```csharp
var data = await _api.For("/protected/data")
    .UsingAuth("Bearer your-token-here")
    .Get()
    .ShouldReturn<ProtectedData>();
```

The `UsingAuth()` method automatically adds the `Authorization` header. If you just pass a token (no "Bearer" prefix), it assumes Bearer authentication.

> **🔐 Advanced Auth:** For complex authentication scenarios, see the [Authentication Guide](authentication.md) for auth providers, caching, and per-user tokens.

---

## Query Parameters and Path Parameters

Working with parameters is straightforward:

```csharp
// Query parameters
var posts = await _api.For("/posts")
    .WithQueryParam("userId", 1)
    .WithQueryParams(new { page = 1, limit = 10 })
    .Get()
    .ShouldReturn<List<Post>>();

// Path parameters
var user = await _api.For("/users/{id}")
    .WithPathParam("id", 123)
    .Get()
    .ShouldReturn<User>();
```

> **📝 Request Building:** Learn about all available request building options in the [Request Building Guide](request-building.md), including headers, cookies, and more.

---

## Validation and Assertions

NaturalApi's `ShouldReturn` method handles validation declaratively:

```csharp
// Type validation only
.ShouldReturn<User>()

// Status code validation
.ShouldReturn(status: 200)

// Type and status
.ShouldReturn<User>(status: 201)

// Body validation with lambda
.ShouldReturn<User>(body: user => user.Name == "John")

// Complete validation
.ShouldReturn<User>(
    status: 201,
    body: user => user.Id > 0 && !string.IsNullOrEmpty(user.Email),
    headers: headers => headers.ContainsKey("Location")
)
```

> **✅ Assertions Guide:** See the complete [Assertions Guide](assertions.md) for all validation patterns and advanced assertion techniques.

---

## Error Handling

When things go wrong, NaturalApi gives you readable error messages:

```csharp
try
{
    await _api.For("/nonexistent")
        .Get()
        .ShouldReturn(status: 200);
}
catch (ApiAssertionException ex)
{
    // Error message: "Expected status 200 but got 404 for GET /nonexistent"
    Console.WriteLine(ex.Message);
}
```

> **🚨 Error Handling:** Learn about all exception types, debugging techniques, and troubleshooting in the [Error Handling Guide](error-handling.md).

---

## Next Steps

Now that you have the basics, explore these topics:

- **[Configuration](configuration.md)** - Setting up base URLs, timeouts, and default headers
- **[Authentication](authentication.md)** - Advanced auth patterns, caching, and per-user tokens
- **[Request Building](request-building.md)** - Headers, cookies, timeouts, and more
- **[HTTP Verbs](http-verbs.md)** - Complete guide to GET, POST, PUT, PATCH, DELETE
- **[Assertions](assertions.md)** - All the ways to validate responses
- **[Testing Guide](testing-guide.md)** - Unit testing with mocks and integration testing
- **[Examples](examples.md)** - Real-world scenarios and complete examples

---

## Common Patterns

Here are some patterns you'll use frequently:

### Simple GET
```csharp
var data = await api.For("/endpoint").Get().ShouldReturn<DataType>();
```

### POST with Validation
```csharp
var result = await api.For("/endpoint")
    .Post(payload)
    .ShouldReturn<ResultType>(status: 201);
```

### Authenticated Request (New Way)
```csharp
var data = await api.For("/protected")
    .AsUser("username", "password")
    .Get()
    .ShouldReturn<DataType>();
```

### Authenticated Request (Token Way)
```csharp
var data = await api.For("/protected")
    .UsingAuth("Bearer token")
    .Get()
    .ShouldReturn<DataType>();
```

### With Parameters
```csharp
var data = await api.For("/search")
    .WithQueryParam("q", "search term")
    .Get()
    .ShouldReturn<SearchResults>();
```

---

## Troubleshooting

If you run into issues:

1. **Check your endpoint URL** - Make sure it's correct and accessible
2. **Verify authentication** - Ensure tokens are valid and properly formatted
3. **Check content types** - Some APIs require specific headers
4. **See [Troubleshooting](troubleshooting.md)** for common issues and solutions

---

## Need Help?

- Check the [API Reference](api-reference.md) for complete method documentation
- Look at [Examples](examples.md) for real-world scenarios
- Review [Error Handling](error-handling.md) for debugging failed requests
- See [Contributing](contributing.md) if you want to contribute to NaturalApi

---

## Related Topics

- **[Configuration](configuration.md)** - Setting up base URLs, timeouts, and DI
- **[HTTP Verbs](http-verbs.md)** - Complete guide to GET, POST, PUT, PATCH, DELETE
- **[Testing Guide](testing-guide.md)** - Unit testing with mocks and integration testing
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions
- **[Philosophy & Design Principles](philosophyanddesignprinciples.md)** - Why NaturalApi works the way it does

Welcome to NaturalApi! Your API tests are about to become much more readable.
