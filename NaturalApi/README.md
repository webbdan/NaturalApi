# NaturalApi

## Fluent API testing that actually reads like English

> **NaturalApi** turns your API tests into sentences you can *read aloud*. No boilerplate. No ceremony. Just clarity.

---

## Why another API library?

Because this...

```csharp
var client = new HttpClient();
var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1");
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
var response = await client.SendAsync(request);
var content = await response.Content.ReadAsStringAsync();
var user = JsonSerializer.Deserialize<User>(content);
Assert.AreEqual(200, (int)response.StatusCode);
Assert.IsNotNull(user);
```

…is the kind of code people write once, copy forever, and never want to look at again.

Now read this:

```csharp
var user = await Api.For("/users/1")
    .UsingAuth("Bearer token")
    .Get()
    .ShouldReturn<User>();
```

That's not just cleaner...it's readable.
It says exactly what it does. And when you come back to it six months later, you'll still know what it means.

---

## Quick Start

```bash
dotnet add package NaturalApi
```

```csharp
using NaturalApi;

// Simple GET request
var users = await Api.For("https://api.example.com/users")
    .Get()
    .ShouldReturn<List<User>>();

// POST with authentication
var newUser = await Api.For("/users")
    .UsingAuth("Bearer your-token")
    .Post(new { name = "John", email = "john@example.com" })
    .ShouldReturn<User>(status: 201);
```

[**Get started →**](documentation/getting-started.md)

---

## Documentation

### Getting Started
- [**Getting Started**](documentation/getting-started.md) - Installation, first API call, basic setup
- [**Configuration**](documentation/configuration.md) - Base URLs, timeouts, default headers, DI setup
- [**Examples**](documentation/examples.md) - Real-world scenarios and complete examples

### Core Features
- [**Request Building**](documentation/request-building.md) - Headers, query params, path params, cookies
- [**HTTP Verbs**](documentation/http-verbs.md) - GET, POST, PUT, PATCH, DELETE with examples
- [**Assertions**](documentation/assertions.md) - ShouldReturn variations, validation patterns
- [**Authentication**](documentation/authentication.md) - Auth providers, caching, per-user tokens

### Advanced Topics
- [**Error Handling**](documentation/error-handling.md) - Exception types, debugging, troubleshooting
- [**Testing Guide**](documentation/testing-guide.md) - Unit testing with mocks, integration testing
- [**Extensibility**](documentation/extensibility.md) - Custom executors, validators, auth providers

### Reference
- [**API Reference**](documentation/api-reference.md) - Complete interface and class documentation
- [**Troubleshooting**](documentation/troubleshooting.md) - Common issues and solutions
- [**Contributing**](documentation/contributing.md) - Architecture internals and contribution guidelines

### Design & Philosophy
- [**Philosophy & Design Principles**](documentation/philosophyanddesignprinciples.md) - Core design philosophy
- [**Fluent Syntax Reference**](documentation/fluentsyntax.md) - Complete grammar and method reference
- [**Dependency Injection Guide**](documentation/di.md) - DI patterns and ServiceCollectionExtensions
- [**Architecture Overview**](documentation/architectureanddesign.md) - Internal design and implementation

---

## The Core Grammar

Every test reads like this:

```
Api.For(endpoint)
   .WithHeaders(...)      // Optional
   .WithQueryParams(...)  // Optional
   .UsingAuth(...)        // Optional
   .<HttpVerb>(body)      // GET, POST, PUT, etc.
   .ShouldReturn<T>(...)  // Validate response
```

Or in practice:

```csharp
await Api.For("/orders/123")
    .UsingAuth("Bearer token")
    .Get()
    .ShouldReturn<Order>(status: 200, body: o => o.Total > 0);
```

You can actually read that aloud. And it still compiles.

> **📚 Learn More:** See the complete [Fluent Syntax Reference](documentation/fluentsyntax.md) for all available methods and patterns.

---

## Five things you can express in one line

```csharp
// 1. Simple GET
await Api.For("/users").Get().ShouldReturn<List<User>>();

// 2. POST with validation
await Api.For("/users").Post(newUser).ShouldReturn<User>(status: 201);

// 3. Authenticated request
await Api.For("/protected").UsingAuth("Bearer token").Get();

// 4. Query parameters
await Api.For("/search").WithQueryParam("q", "api testing").Get();

// 5. Delete with assertion
await Api.For("/users/1").Delete().ShouldReturn(204);
```

Readable, predictable, and type-safe.
Because testing APIs shouldn't feel like writing networking code.

> **🔗 Related:** Learn about [HTTP Verbs](documentation/http-verbs.md), [Assertions](documentation/assertions.md), and [Authentication](documentation/authentication.md) for more advanced patterns.

---

## License

MIT. Do what you want, just don't ruin the readability.

---

## Contributing

Good ideas welcome. Over-engineering isn't. See our [Contributing Guide](documentation/contributing.md) for details.

---

## Related Topics

- **[Getting Started](documentation/getting-started.md)** - Your first API call and basic setup
- **[Configuration](documentation/configuration.md)** - Base URLs, timeouts, and DI setup  
- **[Examples](documentation/examples.md)** - Real-world scenarios and complete examples
- **[Troubleshooting](documentation/troubleshooting.md)** - Common issues and solutions
- **[API Reference](documentation/api-reference.md)** - Complete interface documentation
