# Dependency Injection Guide

> NaturalApi provides flexible dependency injection patterns to fit any application architecture. This guide shows you 8 different ways to register NaturalApi, from ultra-simple to highly customized.

---

## Table of Contents

- [Quick Start](#quick-start)
- [8 DI Registration Patterns](#8-di-registration-patterns)
- [Which Pattern Should I Use?](#which-pattern-should-i-use)
- [Advanced Patterns](#advanced-patterns)
- [Multiple API Instances](#multiple-api-instances)
- [Best Practices](#best-practices)

---

## Quick Start

The simplest way to get started with NaturalApi and DI:

```csharp
// Program.cs or Startup.cs
services.AddNaturalApi();

// Use in your services
public class UserService
{
    private readonly IApi _api;
    
    public UserService(IApi api)
    {
        _api = api;
    }
    
    public async Task<List<User>> GetUsers()
    {
        return await _api.For("https://api.example.com/users")
            .Get()
            .ShouldReturn<List<User>>();
    }
}
```

That's it! NaturalApi is now registered and ready to use.

---

## 8 DI Registration Patterns

NaturalApi offers 8 different registration patterns, from ultra-simple to highly customized. Choose the one that fits your needs.

### Pattern 1: Ultra Simple (No Configuration)

**When to use:** Quick prototyping, simple applications, or when you want to use absolute URLs.

```csharp
services.AddNaturalApi();

// Usage - must use absolute URLs
var api = serviceProvider.GetRequiredService<IApi>();
var result = api.For("https://api.example.com/users").Get();
```

**Pros:** Zero configuration, works immediately
**Cons:** Must use absolute URLs, no base URL convenience

### Pattern 2: With Base URL

**When to use:** When you have a single API with a consistent base URL.

```csharp
services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl("https://api.example.com"));

// Usage - can use relative URLs
var api = serviceProvider.GetRequiredService<IApi>();
var result = api.For("/users").Get();
```

**Pros:** Clean relative URLs, simple setup
**Cons:** No authentication, single API only

### Pattern 3: With Auth Provider

**When to use:** When you need authentication but want to use absolute URLs.

```csharp
services.AddNaturalApi(NaturalApiConfiguration.WithAuth(myAuthProvider));

// Usage - must use absolute URLs but with auth
var api = serviceProvider.GetRequiredService<IApi>();
var result = api.For("https://api.example.com/protected").AsUser("user", "pass").Get();
```

**Pros:** Authentication included, flexible URL usage
**Cons:** Must use absolute URLs, auth provider manages its own URLs

### Pattern 4: With Base URL and Auth (Recommended)

**When to use:** Most common pattern - single API with authentication.

```csharp
services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
    "https://api.example.com", 
    myAuthProvider));

// Usage - clean relative URLs with authentication
var api = serviceProvider.GetRequiredService<IApi>();
var result = api.For("/protected").AsUser("user", "pass").Get();
```

**Pros:** Best of both worlds - clean URLs and authentication
**Cons:** Single API only

### Pattern 5: With Named HttpClient

**When to use:** When you need custom HttpClient configuration.

```csharp
// Configure named HttpClient
services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(60);
});

services.AddNaturalApi(NaturalApiConfiguration.WithHttpClientAndAuth(
    "MyApiClient", 
    myAuthProvider));

// Usage
var api = serviceProvider.GetRequiredService<IApi>();
var result = api.For("/users").AsUser("user", "pass").Get();
```

**Pros:** Full HttpClient control, authentication included
**Cons:** More setup required

### Pattern 6: With Configuration

**When to use:** When you want to read settings from configuration files.

```csharp
// appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "https://api.example.com",
    "AuthBaseUrl": "https://auth.example.com"
  }
}

// Registration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

services.AddSingleton<IConfiguration>(configuration);

var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
    apiBaseUrl!,
    new SimpleCustomAuthProvider(
        new HttpClient { BaseAddress = new Uri(configuration["ApiSettings:AuthBaseUrl"]!) },
        "/auth/login")));
```

**Pros:** Environment-specific configuration, externalized settings
**Cons:** More complex setup

### Pattern 7: With Factory (Maximum Flexibility)

**When to use:** When you need complete control over the API instance creation.

```csharp
services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri("https://auth.example.com");
});

services.AddNaturalApi(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("ApiClient");
    var authHttpClient = httpClientFactory.CreateClient("AuthClient");
    var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
    var defaults = new DefaultApiDefaults(authProvider: authProvider);
    return new Api(defaults, httpClient);
});
```

**Pros:** Complete control, can use any constructor
**Cons:** Most complex, requires understanding of internals

### Pattern 8: With Custom API

**When to use:** When you need a custom API implementation with advanced features.

```csharp
services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri("https://auth.example.com");
});

services.AddNaturalApi<SimpleCustomAuthProvider, CustomApi>(
    "ApiClient",
    provider => new SimpleCustomAuthProvider(
        provider.GetRequiredService<IHttpClientFactory>().CreateClient("AuthClient"),
        "/auth/login"),
    provider =>
    {
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ApiClient");
        var executor = new HttpClientExecutor(httpClient);
        var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
        return new CustomApi(executor, defaults, httpClient);
    });
```

**Pros:** Custom API implementation, advanced features
**Cons:** Most complex, requires custom API class

---

## Which Pattern Should I Use?

Use this decision tree to choose the right pattern:

```
Do you need authentication?
├─ No
│  ├─ Do you want to use relative URLs?
│  │  ├─ Yes → Pattern 2 (With Base URL)
│  │  └─ No → Pattern 1 (Ultra Simple)
│  └─ Do you need custom HttpClient config?
│     ├─ Yes → Pattern 5 (With Named HttpClient)
│     └─ No → Pattern 2 (With Base URL)
└─ Yes
   ├─ Do you want to use relative URLs?
   │  ├─ Yes → Pattern 4 (With Base URL and Auth) ⭐ RECOMMENDED
   │  └─ No → Pattern 3 (With Auth Provider)
   ├─ Do you need configuration from files?
   │  └─ Yes → Pattern 6 (With Configuration)
   ├─ Do you need complete control?
   │  └─ Yes → Pattern 7 (With Factory)
   └─ Do you need custom API implementation?
      └─ Yes → Pattern 8 (With Custom API)
```

### Quick Recommendations

- **Getting started:** Pattern 1 (Ultra Simple)
- **Most applications:** Pattern 4 (With Base URL and Auth)
- **Multiple environments:** Pattern 6 (With Configuration)
- **Custom requirements:** Pattern 7 (With Factory)

---

## Advanced Patterns

### Multiple API Instances

You can register multiple API instances for different services:

```csharp
// User API
services.AddHttpClient("UserApi", client =>
{
    client.BaseAddress = new Uri("https://user-api.example.com");
});

services.AddNaturalApi("UserApi", NaturalApiConfiguration.WithAuth(userAuthProvider));

// Order API
services.AddHttpClient("OrderApi", client =>
{
    client.BaseAddress = new Uri("https://order-api.example.com");
});

services.AddNaturalApi("OrderApi", NaturalApiConfiguration.WithAuth(orderAuthProvider));

// Usage in services
public class OrderService
{
    private readonly IApi _userApi;
    private readonly IApi _orderApi;
    
    public OrderService(
        [FromKeyedServices("UserApi")] IApi userApi,
        [FromKeyedServices("OrderApi")] IApi orderApi)
    {
        _userApi = userApi;
        _orderApi = orderApi;
    }
}
```

### Environment-Specific Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNaturalApiForEnvironment(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        return environment switch
        {
            "Development" => services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl("https://dev-api.example.com")),
            "Staging" => services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
                "https://staging-api.example.com", 
                new DevAuthProvider())),
            "Production" => services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
                "https://api.example.com", 
                new ProductionAuthProvider())),
            _ => services.AddNaturalApi()
        };
    }
}
```

### Scoped vs Singleton Registration

```csharp
// For stateless operations (recommended)
services.AddScoped<IApi>(provider => /* factory */);

// For stateful operations (like session-based auth)
services.AddScoped<IApi>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var session = provider.GetRequiredService<ISessionService>();
    return new Api(new HttpClientExecutor(httpClient), new SessionApiDefaults(session));
});
```

---

## Multiple API Instances

### Using Keyed Services (.NET 8+)

```csharp
// Register multiple APIs
services.AddNaturalApi("UserApi", NaturalApiConfiguration.WithBaseUrl("https://user-api.example.com"));
services.AddNaturalApi("OrderApi", NaturalApiConfiguration.WithBaseUrl("https://order-api.example.com"));

// Use in services
public class OrderService
{
    private readonly IApi _userApi;
    private readonly IApi _orderApi;
    
    public OrderService(
        [FromKeyedServices("UserApi")] IApi userApi,
        [FromKeyedServices("OrderApi")] IApi orderApi)
    {
        _userApi = userApi;
        _orderApi = orderApi;
    }
    
    public async Task<Order> CreateOrderAsync(int userId, OrderRequest request)
    {
        // Verify user exists
        var user = await _userApi.For($"/users/{userId}")
            .Get()
            .ShouldReturn<User>();
            
        // Create order
        var order = await _orderApi.For("/orders")
            .Post(request)
            .ShouldReturn<Order>();
            
        return order;
    }
}
```

### Using Factory Pattern

```csharp
public interface IApiFactory
{
    IApi CreateUserApi();
    IApi CreateOrderApi();
}

public class ApiFactory : IApiFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public ApiFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IApi CreateUserApi()
    {
        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("UserApi");
        return new Api(new HttpClientExecutor(httpClient), new UserApiDefaults());
    }
    
    public IApi CreateOrderApi()
    {
        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("OrderApi");
        return new Api(new HttpClientExecutor(httpClient), new OrderApiDefaults());
    }
}

// Registration
services.AddHttpClient("UserApi", client => client.BaseAddress = new Uri("https://user-api.example.com"));
services.AddHttpClient("OrderApi", client => client.BaseAddress = new Uri("https://order-api.example.com"));
services.AddSingleton<IApiFactory, ApiFactory>();
```

---

## Best Practices

### 1. Choose the Right Pattern

- **Start simple:** Use Pattern 1 or 2 for new projects
- **Add complexity gradually:** Move to Pattern 4 when you need authentication
- **Use configuration:** Pattern 6 for multiple environments
- **Custom only when needed:** Patterns 7 and 8 for special requirements

### 2. HttpClient Configuration

```csharp
// Good: Configure HttpClient properly
services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
});

// Bad: Using default HttpClient without configuration
services.AddHttpClient();
```

### 3. Authentication Provider Design

```csharp
// Good: Auth provider manages its own URLs
public class MyAuthProvider : IApiAuthProvider
{
    private readonly HttpClient _httpClient;
    
    public MyAuthProvider(HttpClient httpClient)
    {
        _httpClient = httpClient; // Pre-configured with auth service URL
    }
    
    public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
    {
        // Auth provider knows its own endpoint
        var response = await _httpClient.PostAsync("/auth/login", content);
        // ...
    }
}
```

### 4. Configuration Management

```csharp
// Good: Use strongly-typed configuration
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthBaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

// Bad: Magic strings in configuration
var baseUrl = configuration["SomeRandomKey:BaseUrl"];
```

### 5. Testing Considerations

```csharp
// Good: Easy to mock in tests
public class UserService
{
    private readonly IApi _api;
    
    public UserService(IApi api)
    {
        _api = api;
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        return await _api.For($"/users/{id}")
            .Get()
            .ShouldReturn<User>();
    }
}

// In tests
var mockApi = new Mock<IApi>();
var userService = new UserService(mockApi.Object);
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic setup and your first API call
- **[Configuration](configuration.md)** - Setting up base URLs, timeouts, and default headers
- **[Authentication](authentication.md)** - Setting up authentication providers
- **[Testing Guide](testing-guide.md)** - Testing with dependency injection
- **[Examples](examples.md)** - Real-world DI scenarios and patterns
- **[API Reference](api-reference.md)** - Complete ServiceCollectionExtensions reference
- **[Troubleshooting](troubleshooting.md)** - Common DI issues and solutions
- **[Architecture Overview](architectureanddesign.md)** - Internal design and implementation details

---

## ServiceCollectionExtensions Reference

### Basic Registration Methods

```csharp
// Ultra simple
services.AddNaturalApi();

// With configuration
services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl("https://api.example.com"));

// With auth provider
services.AddNaturalApi(NaturalApiConfiguration.WithAuth(myAuthProvider));

// With both
services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
    "https://api.example.com", 
    myAuthProvider));
```

### Advanced Registration Methods

```csharp
// With named HttpClient
services.AddNaturalApi("MyApiClient", NaturalApiConfiguration.WithAuth(myAuthProvider));

// With factory
services.AddNaturalApi(provider => new Api(/* custom setup */));

// With custom API implementation
services.AddNaturalApi<MyAuthProvider, MyCustomApi>(
    "MyApiClient",
    provider => new MyAuthProvider(/* setup */),
    provider => new MyCustomApi(/* setup */));
```

### Helper Methods

```csharp
// Convenience method for base URL + auth
services.AddNaturalApiWithBaseUrl("https://api.example.com", myAuthProvider);
```

This covers all the dependency injection patterns available in NaturalApi. Choose the pattern that best fits your application's needs, and don't hesitate to start simple and evolve as requirements grow.