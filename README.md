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

That’s not just cleaner, it’s readable.
It says exactly what it does. And when you come back to it six months later, you’ll still know what it means.

---

## Why you’ll actually keep using it

Lots of libraries make the *first* test look neat. NaturalApi keeps your *hundredth* one readable.

* No `HttpClientFactory` plumbing
* No hidden `.Build()` calls
* No “where did this token come from” moments
  Just straight, expressive flow.

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
Because testing APIs shouldn’t feel like writing networking code.

---

## How to install

```bash
dotnet add package NaturalApi
```

---

## Assertions that read like sentences

The `ShouldReturn` method handles both verification and clarity:

```csharp
// Type only
.ShouldReturn<User>()

// Status code
.ShouldReturn(status: 201)

// Type and status
.ShouldReturn<User>(status: 200)

// Validate body
.ShouldReturn<User>(body: u => u.Name == "John")

// All at once
.ShouldReturn<User>(
    status: 201,
    body: u => u.Id > 0 && !string.IsNullOrEmpty(u.Email),
    headers: h => h.ContainsKey("Location")
);
```

Every validation reads like what you’d say aloud:

> “It should return a user, with status 201, whose email isn’t empty.”

---

## Using Dependency Injection

NaturalApi was designed for DI- not bolted onto it.

```csharp
// Program.cs or Startup.cs
services.AddNaturalApi(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders = new Dictionary<string, string>
    {
        ["Accept"] = "application/json"
    };
});

services.AddSingleton<IApiAuthProvider, MyAuthProvider>();
```

Then in your code:

```csharp
public class UserController
{
    private readonly IApi _api;

    public UserController(IApi api)
    {
        _api = api;
    }

    public async Task<User> GetUser(int id)
    {
        return await _api.For($"/users/{id}")
            .Get()
            .ShouldReturn<User>();
    }
}
```

No client factories. No setup churn. Just inject and use.

---

## Testing without pain

### Unit tests

```csharp
[TestClass]
public class UserServiceTests
{
    private MockHttpExecutor _mockExecutor;
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new MockHttpExecutor();
        _api = new Api(_mockExecutor);
    }

    [TestMethod]
    public async Task GetUser_Should_Return_User()
    {
        _mockExecutor.SetupResponse("/users/1", HttpStatusCode.OK, new { id = 1, name = "John" });

        var user = await _api.For("/users/1")
            .Get()
            .ShouldReturn<User>();

        Assert.AreEqual("John", user.Name);
    }
}
```

### Integration tests

```csharp
[TestClass]
public class UserApiIntegrationTests
{
    [TestMethod]
    public async Task Create_User_Should_Return_201()
    {
        var newUser = await Api.For("https://jsonplaceholder.typicode.com/users")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(new { name = "John Doe", email = "john@example.com" })
            .ShouldReturn<User>(status: 201);

        Assert.IsNotNull(newUser);
        Assert.IsTrue(newUser.Id > 0);
    }
}
```

Mocks if you need speed. Live calls if you need confidence.

---

## Under the hood

NaturalApi is built on four simple layers:

* **Fluent DSL** – what you write
* **Context Builders** – immutable state composition
* **Execution Engine** – HTTP and deserialisation
* **Validation Layer** – readable, declarative assertions

Each layer has one job. Nothing hidden, nothing magic, nothing you’ll regret later.

---

## Design philosophy

1. **Speak like a human** – if you can’t say it, don’t write it
2. **Mirror tester logic** – `For → With → Using → Do → ShouldReturn`
3. **Optional and predictable** – no surprises, no “setup or die”
4. **Readable failures** – assertions that explain themselves
5. **Fluent, not fragile** – no builders, no boilerplate

---

## Why it exists

I built NaturalApi because writing API tests shouldn’t feel like wiring up a web server.
You shouldn’t need five lines of setup just to say “this worked.”

If your tests read like English and fail like humans speak, you’ll actually read them again.

---

## Documentation

### Getting Started
- [**Getting Started**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/getting-started.md) - Installation, first API call, basic setup
- [**Configuration**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/configuration.md) - Base URLs, timeouts, default headers, DI setup
- [**Examples**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/examples.md) - Real-world scenarios and complete examples

### Core Features
- [**Request Building**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/request-building.md) - Headers, query params, path params, cookies
- [**HTTP Verbs**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/http-verbs.md) - GET, POST, PUT, PATCH, DELETE with examples
- [**Assertions**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/assertions.md) - ShouldReturn variations, validation patterns
- [**Authentication**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/authentication.md) - Auth providers, caching, per-user tokens

### Advanced Topics
- [**Error Handling**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/error-handling.md) - Exception types, debugging, troubleshooting
- [**Testing Guide**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/testing-guide.md) - Unit testing with mocks, integration testing
- [**Extensibility**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/extensibility.md) - Custom executors, validators, auth providers
- [**Reporting**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/reporting.md) - Configurable reporters, DI factory and examples


### Reference
- [**API Reference**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/api-reference.md) - Complete interface and class documentation
- [**Troubleshooting**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/troubleshooting.md) - Common issues and solutions
- [**Contributing**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/contributing.md) - Architecture internals and contribution guidelines

### Design & Philosophy
- [**Philosophy & Design Principles**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/philosophyanddesignprinciples.md) - Core design philosophy
- [**Fluent Syntax Reference**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/fluentsyntax.md) - Complete grammar and method reference
- [**Dependency Injection Guide**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/di.md) - DI patterns and ServiceCollectionExtensions
- [**Architecture Overview**](https://github.com/webbdan/NaturalApi/tree/main/NaturalApi/documentation/architectureanddesign.md) - Internal design and implementation

---

## License

MIT. Do what you want, just don't ruin the readability.

---

## Contributing

Good ideas welcome. Over-engineering isn't.