## 🔧 Developer Spec: Auth Integration in NaturalApi

### **Overview**

Authentication is managed via **`IApiAuthProvider`**, a simple DI-resolvable interface that provides tokens to be automatically added to outgoing requests.

Developers can:

* Register their own implementation in DI.
* Decide how tokens are fetched (static, cached, per-user, etc.).
* Override or disable auth per request (`.WithoutAuth()`).

NaturalApi itself provides a default `HttpClientAuthExecutor` that respects whatever `IApiAuthProvider` is registered.

---

### **Interfaces**

#### **IApiAuthProvider**

The contract for all authentication providers.

```csharp
public interface IApiAuthProvider
{
    /// <summary>
    /// Returns a valid auth token (without the scheme).
    /// Returning null means no auth header will be added.
    /// </summary>
    Task<string?> GetAuthTokenAsync(string? username = null);
}
```

---

### **IApiDefaultsProvider**

Defaults can include base URL, timeout, default headers, and optionally an auth provider.

```csharp
public interface IApiDefaultsProvider
{
    Uri? BaseUri { get; }
    IDictionary<string, string> DefaultHeaders { get; }
    TimeSpan Timeout { get; }
    IApiAuthProvider? AuthProvider { get; }
}
```

Default implementations of both are registered via DI. If none is provided, NaturalApi just skips auth entirely.

---

## ⚙️ Implementation Example

### **ServiceCollectionExtensions**

NaturalApi provides extension methods for easy DI registration:

```csharp
// Basic registration
services.AddNaturalApi();

// With configuration
services.AddNaturalApi(options =>
{
    options.RegisterDefaults = true;
});

// With custom defaults provider
services.AddNaturalApi<MyDefaultsProvider>(new MyDefaultsProvider());

// With auth provider
services.AddNaturalApiWithAuth<MyAuthProvider>(new MyAuthProvider());

// With both defaults and auth
services.AddNaturalApiWithAuth<MyDefaults, MyAuth>(new MyDefaults(), new MyAuth());

// With custom factory
services.AddNaturalApi(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
    return new Api(new HttpClientExecutor(httpClient), defaults);
});
```

### **Manual DI Setup**

```csharp
services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
services.AddSingleton<IApiAuthProvider, CachingAuthProvider>();
services.AddHttpClient();
services.AddSingleton<IApi, Api>();
```

### **DefaultApiDefaults.cs**

```csharp
public class DefaultApiDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new("https://api.mycompany.com/");
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        { "Accept", "application/json" }
    };
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider { get; }

    public DefaultApiDefaults(IApiAuthProvider? authProvider = null)
    {
        AuthProvider = authProvider;
    }
}
```

---

### **Example Auth Provider**

```csharp
public class CachingAuthProvider : IApiAuthProvider
{
    private string? _token;
    private DateTime _expires;

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        if (_token == null || DateTime.UtcNow > _expires)
        {
            var newToken = await FetchNewTokenAsync();
            _token = newToken.Token;
            _expires = DateTime.UtcNow.AddMinutes(newToken.ExpiresInMinutes - 1);
        }
        return _token;
    }

    private Task<(string Token, int ExpiresInMinutes)> FetchNewTokenAsync()
        => Task.FromResult(("abc123", 30));
}
```

---

## 🚀 Usage in Tests

### **1. Using DI to Create Api Instance**

```csharp
var api = serviceProvider.GetRequiredService<IApi>();
```

Or, if you’re not using DI in tests:

```csharp
var api = new NaturalApi(httpClient, defaults);
```

---

### **2. Simple Authenticated Call**

```csharp
var resp = await api
    .For("/users/me")
    .Get()
    .ShouldReturn<UserResponse>();
```

NaturalApi automatically:

1. Resolves the base URI (`https://api.mycompany.com/users/me`).
2. Fetches the token via `IApiAuthProvider.GetAuthTokenAsync()`.
3. Adds the header: `Authorization: Bearer abc123`.

You didn’t need to touch a thing.

---

### **3. Call Without Auth**

```csharp
var resp = await api
    .For("/public/info")
    .WithoutAuth()
    .Get()
    .ShouldReturn<PublicInfo>();
```

`.WithoutAuth()` simply skips invoking the auth provider for that call.

---

### **4. Per-User Token Example**

If you want multi-user tests:

```csharp
var resp = await api
    .For("/users")
    .AsUser("dan")
    .Get()
    .ShouldReturn<UserList>();
```

`AsUser()` sets a contextual username that’s passed into `IApiAuthProvider.GetAuthTokenAsync("dan")`.

---

## 🧠 Internal Request Flow

Here’s the high-level sequence inside the NaturalApi engine:

1. **Build Phase:**
   Collect all configured values: endpoint, headers, body, timeout, etc.

2. **Resolve URI:**
   Combine `BaseUri` and relative path (unless absolute provided).

3. **Apply Defaults:**
   Add global headers from `IApiDefaultsProvider`.

4. **Auth Resolution:**

   ```csharp
   if (!_suppressAuth && _defaults.AuthProvider != null)
   {
       var token = await _defaults.AuthProvider.GetAuthTokenAsync(_user);
       if (!string.IsNullOrEmpty(token))
           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
   }
   ```

5. **Execute Request:**
   Use injected `IHttpExecutor` (defaulting to `HttpClientExecutor`).

6. **Return Typed Response:**
   Deserialise response body into `T` in `.ShouldReturn<T>()`.

---

## 🧩 Why It Works

| Principle            | Implementation                                   |
| -------------------- | ------------------------------------------------ |
| **Natural syntax**   | `.For(...).Get().ShouldReturn<T>()`              |
| **No boilerplate**   | Auth handled automatically via DI                |
| **Simple overrides** | `.WithoutAuth()` or `.AsUser()` per call         |
| **Flexible**         | Works with any token source                      |
| **Clean separation** | Core doesn’t know or care how tokens are managed |

---

## ✅ Example End-to-End Usage

```csharp
[Fact]
public async Task Get_Current_User_Should_Return_Valid_Response()
{
    var resp = await api
        .For("/users/me")
        .Get()
        .ShouldReturn<UserResponse>();

    resp.Name.ShouldBe("Dan");
    resp.Role.ShouldBe("Tester");
}
```

No boilerplate. No headers scattered around. No static token mess. Just clean, natural English-style API testing.

---

## 🔧 Advanced DI Scenarios

### **Multi-Environment Configuration**

```csharp
public class EnvironmentApiDefaults : IApiDefaultsProvider
{
    private readonly string _environment;
    private readonly IConfiguration _configuration;

    public EnvironmentApiDefaults(IConfiguration configuration)
    {
        _configuration = configuration;
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
        ["X-Environment"] = _environment,
        ["X-Version"] = _configuration["ApiSettings:Version"]
    };

    public TimeSpan Timeout => _environment == "Production" 
        ? TimeSpan.FromSeconds(30) 
        : TimeSpan.FromSeconds(60);

    public IApiAuthProvider? AuthProvider => _environment != "Development" 
        ? new EnvironmentAuthProvider(_configuration)
        : null;
}
```

### **Scoped Authentication**

```csharp
public class UserScopedAuthProvider : IApiAuthProvider
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokenService;

    public UserScopedAuthProvider(ICurrentUserService currentUser, ITokenService tokenService)
    {
        _currentUser = currentUser;
        _tokenService = tokenService;
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        var user = username != null 
            ? await _currentUser.GetUserAsync(username)
            : _currentUser.GetCurrentUser();
        
        return await _tokenService.GetTokenForUserAsync(user.Id);
    }
}

// Registration
services.AddScoped<IApiAuthProvider, UserScopedAuthProvider>();
services.AddScoped<IApi>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var authProvider = provider.GetRequiredService<IApiAuthProvider>();
    var defaults = new DefaultApiDefaults(authProvider: authProvider);
    return new Api(new HttpClientExecutor(httpClient), defaults);
});
```

### **Multiple API Endpoints**

```csharp
// Register multiple API instances
services.AddHttpClient("UserApi", client =>
{
    client.BaseAddress = new Uri("https://user-api.example.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

services.AddHttpClient("OrderApi", client =>
{
    client.BaseAddress = new Uri("https://order-api.example.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

services.AddNaturalApi("UserApi", provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("UserApi");
    return new Api(new HttpClientExecutor(httpClient));
});

services.AddNaturalApi("OrderApi", provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OrderApi");
    return new Api(new HttpClientExecutor(httpClient));
});

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
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // Get user details from User API
        var user = await _userApi.For($"/users/{request.UserId}")
            .Get()
            .ShouldReturn<User>();
        
        // Create order in Order API
        var order = await _orderApi.For("/orders")
            .Post(request)
            .ShouldReturn<Order>(status: 201);
        
        return order;
    }
}
```

### **Configuration-Based Setup**

```csharp
// appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "https://api.example.com",
    "TimeoutSeconds": 30,
    "DefaultHeaders": {
      "Accept": "application/json",
      "User-Agent": "MyApp/1.0"
    },
    "Auth": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "TokenEndpoint": "https://auth.example.com/token"
    }
  }
}

// Configuration class
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
}

public class AuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
}

// Registration
services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

services.AddNaturalApi(provider =>
{
    var options = provider.GetRequiredService<IOptions<ApiSettings>>().Value;
    var httpClient = provider.GetRequiredService<HttpClient>();
    
    var defaults = new DefaultApiDefaults(
        baseUri: new Uri(options.BaseUrl),
        defaultHeaders: options.DefaultHeaders,
        timeout: TimeSpan.FromSeconds(options.TimeoutSeconds)
    );
    
    var authProvider = new OAuth2AuthProvider(
        options.Auth.ClientId,
        options.Auth.ClientSecret,
        options.Auth.TokenEndpoint,
        httpClient
    );
    
    return new Api(new HttpClientExecutor(httpClient), defaults);
});
```

### **Testing with DI**

```csharp
[TestClass]
public class ApiServiceTests
{
    private ServiceProvider _serviceProvider;
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Register test services
        services.AddHttpClient();
        services.AddSingleton<IApiDefaultsProvider, TestApiDefaults>();
        services.AddSingleton<IApiAuthProvider, TestAuthProvider>();
        services.AddNaturalApi();
        
        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public async Task GetUser_Should_Return_User_With_Auth()
    {
        var user = await _api.For("/users/1")
            .Get()
            .ShouldReturn<User>();

        Assert.IsNotNull(user);
        Assert.AreEqual(1, user.Id);
    }
}

public class TestApiDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri("https://test-api.example.com");
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>
    {
        ["Accept"] = "application/json"
    };
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider => new TestAuthProvider();
}

public class TestAuthProvider : IApiAuthProvider
{
    public Task<string?> GetAuthTokenAsync(string? username = null)
    {
        return Task.FromResult<string?>("test-token");
    }
}
```

---

## 🧠 Best Practices

### **1. Use Appropriate Service Lifetimes**

```csharp
// Singleton for stateless services
services.AddSingleton<IApiDefaultsProvider, MyDefaultsProvider>();

// Scoped for user-specific services
services.AddScoped<IApiAuthProvider, UserScopedAuthProvider>();

// Transient for request-specific services
services.AddTransient<IApi>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    return new Api(new HttpClientExecutor(httpClient));
});
```

### **2. Handle Configuration Changes**

```csharp
public class ConfigurableApiDefaults : IApiDefaultsProvider
{
    private readonly IOptionsMonitor<ApiSettings> _options;

    public ConfigurableApiDefaults(IOptionsMonitor<ApiSettings> options)
    {
        _options = options;
    }

    public Uri? BaseUri => new Uri(_options.CurrentValue.BaseUrl);
    public IDictionary<string, string> DefaultHeaders => _options.CurrentValue.DefaultHeaders;
    public TimeSpan Timeout => TimeSpan.FromSeconds(_options.CurrentValue.TimeoutSeconds);
    public IApiAuthProvider? AuthProvider => null;
}
```

### **3. Use Factory Pattern for Complex Scenarios**

```csharp
public interface IApiFactory
{
    IApi CreateApi(string baseUrl);
    IApi CreateApi(string baseUrl, IApiAuthProvider authProvider);
}

public class ApiFactory : IApiFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiFactory> _logger;

    public ApiFactory(IHttpClientFactory httpClientFactory, ILogger<ApiFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IApi CreateApi(string baseUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var executor = new HttpClientExecutor(httpClient);
        return new Api(executor);
    }

    public IApi CreateApi(string baseUrl, IApiAuthProvider authProvider)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var executor = new AuthenticatedHttpClientExecutor(httpClient);
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        return new Api(executor, defaults);
    }
}

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic setup and usage
- **[Configuration](configuration.md)** - Configuration patterns and setup
- **[Authentication](authentication.md)** - Authentication patterns and providers
- **[Extensibility](extensibility.md)** - Creating custom implementations
- **[Testing Guide](testing-guide.md)** - Testing with DI
- **[API Reference](api-reference.md)** - ServiceCollectionExtensions documentation
- **[Examples](examples.md)** - Real-world DI scenarios
- **[Troubleshooting](troubleshooting.md)** - DI registration issues
- **[Architecture Overview](architectureanddesign.md)** - Internal design and DI integration
```