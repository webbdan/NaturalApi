# Configuration Guide

> NaturalApi offers flexible configuration options to fit your application's needs. This guide covers base URLs, timeouts, default headers, and dependency injection setup.

---

## Table of Contents

- [Basic Configuration](#basic-configuration)
- [Base URL Configuration](#base-url-configuration)
- [Default Headers](#default-headers)
- [Timeouts](#timeouts)
- [Dependency Injection Setup](#dependency-injection-setup)
- [Custom Defaults Provider](#custom-defaults-provider)
- [Advanced Configuration](#advanced-configuration)

---

## Basic Configuration

The simplest way to configure NaturalApi is through the constructor:

```csharp
// With base URL
var api = new Api("https://api.example.com");

// With HttpClient
var httpClient = new HttpClient();
var api = new Api(new HttpClientExecutor(httpClient));
```

---

## Base URL Configuration

### Constructor-based Configuration

```csharp
// Absolute URL - used as-is
var api = new Api("https://api.example.com");

// Relative endpoints are resolved against base URL
var users = await api.For("/users")  // Calls https://api.example.com/users
    .Get()
    .ShouldReturn<List<User>>();
```

### Defaults Provider Configuration

For more complex scenarios, use `IApiDefaultsProvider`:

```csharp
var defaults = new DefaultApiDefaults(
    baseUri: new Uri("https://api.example.com"),
    defaultHeaders: new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["User-Agent"] = "MyApp/1.0"
    },
    timeout: TimeSpan.FromSeconds(30)
);

var api = new Api(new HttpClientExecutor(new HttpClient()), defaults);
```

### Environment-specific Configuration

```csharp
public class EnvironmentApiDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri(Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://api.example.com");
    
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["X-Environment"] = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "development"
    };
    
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    
    public IApiAuthProvider? AuthProvider => null;
}
```

---

## Default Headers

### Setting Default Headers

```csharp
var defaults = new DefaultApiDefaults(
    defaultHeaders: new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["User-Agent"] = "MyApp/1.0",
        ["X-API-Version"] = "v1"
    }
);

var api = new Api(executor, defaults);

// All requests will include these headers automatically
var users = await api.For("/users")
    .Get()  // Includes Accept, User-Agent, X-API-Version headers
    .ShouldReturn<List<User>>();
```

### Overriding Default Headers

```csharp
// Override specific headers per request
var users = await api.For("/users")
    .WithHeader("Accept", "application/xml")  // Overrides default Accept header
    .Get()
    .ShouldReturn<List<User>>();
```

### Dynamic Headers

```csharp
public class DynamicApiDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri("https://api.example.com");
    
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["X-Request-ID"] = Guid.NewGuid().ToString(),  // Unique per request
        ["X-Timestamp"] = DateTime.UtcNow.ToString("O")
    };
    
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider => null;
}
```

---

## Timeouts

### Global Timeout Configuration

```csharp
var defaults = new DefaultApiDefaults(
    timeout: TimeSpan.FromSeconds(60)  // 60 second timeout for all requests
);

var api = new Api(executor, defaults);
```

### Per-Request Timeout

```csharp
// Override timeout for specific requests
var data = await api.For("/slow-endpoint")
    .WithTimeout(TimeSpan.FromMinutes(5))  // 5 minute timeout for this request
    .Get()
    .ShouldReturn<Data>();
```

### Timeout Handling

```csharp
try
{
    var data = await api.For("/timeout-endpoint")
        .WithTimeout(TimeSpan.FromSeconds(1))
        .Get()
        .ShouldReturn<Data>();
}
catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
{
    Console.WriteLine("Request timed out");
}
```

---

## Dependency Injection Setup

### Basic DI Registration

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
        return await _api.For("/users")
            .Get()
            .ShouldReturn<List<User>>();
    }
}
```

> **🔧 DI Guide:** For comprehensive dependency injection patterns, see the [Dependency Injection Guide](di.md) for advanced scenarios and ServiceCollectionExtensions.

### Custom Configuration

```csharp
services.AddNaturalApi(options =>
{
    options.RegisterDefaults = true;  // Register default implementations
});

// Register custom defaults
services.AddSingleton<IApiDefaultsProvider>(provider =>
    new DefaultApiDefaults(
        baseUri: new Uri("https://api.example.com"),
        defaultHeaders: new Dictionary<string, string>
        {
            ["Accept"] = "application/json"
        },
        timeout: TimeSpan.FromSeconds(30)
    ));
```

### With Custom Auth Provider

```csharp
services.AddNaturalApiWithAuth<MyAuthProvider>(new MyAuthProvider());

// Or with custom defaults and auth
services.AddNaturalApiWithAuth<MyDefaults, MyAuthProvider>(
    new MyDefaults(),
    new MyAuthProvider()
);
```

> **🔐 Authentication:** Learn about authentication providers and patterns in the [Authentication Guide](authentication.md).

### Custom Factory

```csharp
services.AddNaturalApi(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
    return new Api(new HttpClientExecutor(httpClient), defaults);
});
```

---

## Custom Defaults Provider

### Creating a Custom Provider

```csharp
public class MyApiDefaults : IApiDefaultsProvider
{
    private readonly IConfiguration _configuration;
    
    public MyApiDefaults(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public Uri? BaseUri => new Uri(_configuration["ApiSettings:BaseUrl"]);
    
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["X-API-Key"] = _configuration["ApiSettings:ApiKey"],
        ["User-Agent"] = _configuration["ApiSettings:UserAgent"]
    };
    
    public TimeSpan Timeout => TimeSpan.FromSeconds(
        _configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 30)
    );
    
    public IApiAuthProvider? AuthProvider => null;
}
```

### Multi-Environment Configuration

```csharp
public class EnvironmentApiDefaults : IApiDefaultsProvider
{
    private readonly string _environment;
    
    public EnvironmentApiDefaults()
    {
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }
    
    public Uri? BaseUri => _environment switch
    {
        "Development" => new Uri("https://dev-api.example.com"),
        "Staging" => new Uri("https://staging-api.example.com"),
        "Production" => new Uri("https://api.example.com"),
        _ => new Uri("https://localhost:5001")
    };
    
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["X-Environment"] = _environment
    };
    
    public TimeSpan Timeout => _environment == "Production" 
        ? TimeSpan.FromSeconds(30) 
        : TimeSpan.FromSeconds(60);
    
    public IApiAuthProvider? AuthProvider => null;
}
```

---

## Advanced Configuration

### Multiple API Endpoints

```csharp
// Register multiple API instances for different services
services.AddNaturalApi("UserApi", provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("UserApi");
    return new Api(new HttpClientExecutor(httpClient), new UserApiDefaults());
});

services.AddNaturalApi("OrderApi", provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OrderApi");
    return new Api(new HttpClientExecutor(httpClient), new OrderApiDefaults());
});

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
}
```

### Configuration with Options Pattern

```csharp
// appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "https://api.example.com",
    "TimeoutSeconds": 30,
    "DefaultHeaders": {
      "Accept": "application/json",
      "User-Agent": "MyApp/1.0"
    }
  }
}

// Configuration class
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}

// Usage
services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

services.AddNaturalApi(provider =>
{
    var options = provider.GetRequiredService<IOptions<ApiSettings>>().Value;
    var defaults = new DefaultApiDefaults(
        baseUri: new Uri(options.BaseUrl),
        defaultHeaders: options.DefaultHeaders,
        timeout: TimeSpan.FromSeconds(options.TimeoutSeconds)
    );
    return new Api(new HttpClientExecutor(new HttpClient()), defaults);
});
```

---

## Best Practices

### 1. Use Environment Variables for Sensitive Data

```csharp
public class SecureApiDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri(Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://localhost");
    
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["Authorization"] = $"Bearer {Environment.GetEnvironmentVariable("API_TOKEN")}"
    };
    
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider => null;
}
```

### 2. Configure HttpClient Properly

```csharp
services.AddHttpClient("ApiClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddNaturalApi(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
    return new Api(new HttpClientExecutor(httpClient));
});
```

### 3. Use Scoped Lifetime for Stateful Operations

```csharp
// For stateful operations (like session-based auth)
services.AddScoped<IApi>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var session = provider.GetRequiredService<ISessionService>();
    return new Api(new HttpClientExecutor(httpClient), new SessionApiDefaults(session));
});
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic setup and your first API call
- **[Authentication](authentication.md)** - Setting up authentication providers
- **[Request Building](request-building.md)** - Per-request configuration options
- **[Testing Guide](testing-guide.md)** - Configuration for testing scenarios
- **[Examples](examples.md)** - Real-world configuration examples
- **[Troubleshooting](troubleshooting.md)** - Common configuration issues
- **[Dependency Injection Guide](di.md)** - Advanced DI patterns and ServiceCollectionExtensions
- **[Architecture Overview](architectureanddesign.md)** - Internal design and implementation details
