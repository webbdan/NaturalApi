# Examples

> Real-world scenarios and complete examples demonstrating NaturalApi in action.

---

## Table of Contents

- [8 Ways to Register NaturalApi](#8-ways-to-register-naturalapi)
- [Complete Integration Testing Setup](#complete-integration-testing-setup)
- [Username/Password Authentication](#usernamepassword-authentication)
- [Multiple Users with Token Caching](#multiple-users-with-token-caching)
- [Configuration-Based Setup](#configuration-based-setup)
- [Testing Without Dependency Injection](#testing-without-dependency-injection)
- [Custom Authentication Service](#custom-authentication-service)
- [Complete CRUD Operations](#complete-crud-operations)
- [Authentication Flow](#authentication-flow)
- [Multi-Step Workflows](#multi-step-workflows)
- [Error Handling Scenarios](#error-handling-scenarios)
- [Testing Patterns](#testing-patterns)
- [Advanced Configuration](#advanced-configuration)
- [Integration Examples](#integration-examples)

---

## 8 Ways to Register NaturalApi

This comprehensive example shows all 8 dependency injection patterns available in NaturalApi, from ultra-simple to highly customized.

```csharp
[TestClass]
public class AllDiPatternsExamples
{
    private WireMockServers _wireMockServers = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Pattern1_UltraSimple_ShouldWork()
    {
        // Pattern 1: Ultra Simple - No configuration, must use absolute URLs
        var services = new ServiceCollection();
        services.AddNaturalApi();

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Must use absolute URLs
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task Pattern2_WithBaseUrl_ShouldWork()
    {
        // Pattern 2: With Base URL - Can use relative URLs
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrl(_wireMockServers.ApiBaseUrl));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs
        var result = api.For("/api/protected").Get();

        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task Pattern3_WithAuthProvider_ShouldWork()
    {
        // Pattern 3: With Auth Provider - Auth provider knows its own URLs
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithAuth(new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Must use absolute URLs but with auth
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Pattern4_WithBaseUrlAndAuth_ShouldWork()
    {
        // Pattern 4: With Both - Best of both worlds (RECOMMENDED)
        var services = new ServiceCollection();
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Pattern5_WithNamedHttpClient_ShouldWork()
    {
        // Pattern 5: With Named HttpClient
        var services = new ServiceCollection();
        
        // Configure named HttpClient
        services.AddHttpClient("MyApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        services.AddHttpClient("MyAuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use named HttpClient with auth
        services.AddNaturalApi(NaturalApiConfiguration.WithHttpClientAndAuth("MyApiClient", new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Pattern6_WithConfiguration_ShouldWork()
    {
        // Pattern 6: With Configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use configuration values
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Pattern7_WithFactory_ShouldWork()
    {
        // Pattern 7: With Factory - Maximum flexibility
        var services = new ServiceCollection();
        
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use factory for maximum control
        services.AddNaturalApi(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            var authHttpClient = httpClientFactory.CreateClient("AuthClient");
            var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
            var defaults = new DefaultApiDefaults(authProvider: authProvider);
            return new Api(defaults, httpClient);
        });

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Pattern8_WithCustomApi_ShouldWork()
    {
        // Pattern 8: With Custom API - Advanced scenarios
        var services = new ServiceCollection();
        
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Register auth provider
        services.AddSingleton<IApiAuthProvider>(new SimpleCustomAuthProvider(
            new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
            "/auth/login"));

        // Register defaults
        services.AddSingleton<IApiDefaultsProvider>(provider =>
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });

        // Use custom API factory
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

        var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IApi>();

        // Can use relative URLs with auth
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
```

### Which Pattern Should I Use?

- **Pattern 1**: Quick prototyping, simple scripts
- **Pattern 2**: Single API, no authentication needed
- **Pattern 3**: Authentication needed, but want to use absolute URLs
- **Pattern 4**: Most applications (RECOMMENDED)
- **Pattern 5**: Need custom HttpClient configuration
- **Pattern 6**: Multiple environments, configuration-driven
- **Pattern 7**: Maximum control over API creation
- **Pattern 8**: Custom API implementation needed

---

## Complete Integration Testing Setup

This example shows how to set up comprehensive integration testing with WireMock, including authentication flows and multiple API endpoints.

```csharp
[TestClass]
public class CompleteIntegrationTestingExample
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        // Configure services with authentication
        var services = new ServiceCollection();
        
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });
        
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Complete_Authentication_Flow_Should_Work()
    {
        // Act - Use the new AsUser() method
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Different_Users_Should_Work()
    {
        // User 1
        var result1 = _api.For("/api/protected").AsUser("testuser", "testpass").Get();
        Assert.AreEqual(200, result1.StatusCode);

        // User 2 (different credentials)
        var result2 = _api.For("/api/protected").AsUser("user2", "pass2").Get();
        Assert.AreEqual(200, result2.StatusCode);
        Assert.IsTrue(result2.RawBody.Contains("Access granted for user2"));
    }

    [TestMethod]
    public async Task Invalid_Credentials_Should_Fail()
    {
        // Act - Invalid credentials
        var result = _api.For("/api/protected").AsUser("invaliduser", "wrongpass").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Unauthorized"));
    }
}
```

---

## Username/Password Authentication

This example demonstrates the complete username/password authentication flow with NaturalApi.

```csharp
[TestClass]
public class UsernamePasswordAuthenticationExample
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        var services = new ServiceCollection();
        
        // Configure auth service
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });
        
        // Register NaturalApi with authentication
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            _wireMockServers.ApiBaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Authenticated_Request_With_Credentials_Should_Work()
    {
        // Act - Just provide username and password
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Without_Credentials_Should_Fail()
    {
        // Act - No credentials provided
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Unauthorized"));
    }

    [TestMethod]
    public async Task Username_Only_Should_Fail()
    {
        // Act - Only username, no password
        var result = _api.For("/api/protected").AsUser("testuser").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Unauthorized"));
    }
}
```

---

## Multiple Users with Token Caching

This example shows how to handle multiple users with token caching using the advanced authentication service pattern.

```csharp
[TestClass]
public class MultipleUsersWithTokenCachingExample
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        var services = new ServiceCollection();

        // Configure HttpClient for auth service
        services.AddHttpClient<IUsernamePasswordAuthService, UsernamePasswordAuthService>(client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Configure HttpClient for NaturalApi
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.ApiBaseUrl);
        });

        // Register NaturalApi with custom auth provider
        services.AddNaturalApi<CustomAuthProvider, CustomApi>("ApiClient", 
            provider =>
            {
                var authService = provider.GetRequiredService<IUsernamePasswordAuthService>();
                return new CustomAuthProvider(authService, "defaultuser", "defaultpass");
            },
            provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var executor = new HttpClientExecutor(httpClient);
                var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
                return new CustomApi(executor, defaults, httpClient);
            });

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task First_Time_Authentication_Should_Succeed()
    {
        // Act - Make API call (should trigger authentication)
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Cached_Token_Should_Be_Used_On_Subsequent_Calls()
    {
        // Act - Make first call (triggers authentication)
        var result1 = _api.For("/api/protected").Get();
        
        // Make second call (should use cached token)
        var result2 = _api.For("/api/protected").Get();

        // Assert both calls succeed
        Assert.AreEqual(200, result1.StatusCode);
        Assert.AreEqual(200, result2.StatusCode);
    }

    [TestMethod]
    public async Task Multiple_Users_Should_Have_Separate_Token_Caches()
    {
        // Arrange - Create service provider for second user
        var serviceProvider2 = ServiceConfiguration.ConfigureServices(
            authServerUrl: _wireMockServers.AuthBaseUrl,
            apiServerUrl: _wireMockServers.ApiBaseUrl,
            username: "user2",
            password: "pass2",
            cacheExpiration: TimeSpan.FromMinutes(10));

        var api2 = serviceProvider2.GetRequiredService<IApi>();

        try
        {
            // Act - Make calls with both users
            var result1 = _api.For("/api/protected").Get();
            var result2 = api2.For("/api/protected").Get();

            // Assert both succeed with different responses
            Assert.AreEqual(200, result1.StatusCode);
            Assert.AreEqual(200, result2.StatusCode);
            
            Assert.IsTrue(result1.RawBody.Contains("Access granted"));
            Assert.IsTrue(result2.RawBody.Contains("Access granted for user2"));
        }
        finally
        {
            if (serviceProvider2 is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
```

---

## Configuration-Based Setup

This example shows how to use configuration files with NaturalApi for different environments.

```csharp
[TestClass]
public class ConfigurationBasedSetupExample
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Configuration_From_AppSettings_Should_Work()
    {
        // Arrange - Create configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["ApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl,
                ["ApiSettings:AuthEndpoint"] = "/auth/login"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Configure HttpClient for auth service
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(_wireMockServers.AuthBaseUrl);
        });

        // Use configuration to register NaturalApi
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        services.AddNaturalApi(NaturalApiConfiguration.WithBaseUrlAndAuth(
            apiBaseUrl!,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) },
                "/auth/login")));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Strongly_Typed_Configuration_Should_Work()
    {
        // Arrange - Strongly-typed configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NaturalApiSettings:BaseUrl"] = _wireMockServers.ApiBaseUrl,
                ["NaturalApiSettings:AuthBaseUrl"] = _wireMockServers.AuthBaseUrl,
                ["NaturalApiSettings:AuthEndpoint"] = "/auth/login"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Bind strongly-typed configuration
        var apiSettings = new NaturalApiSettings();
        configuration.GetSection("NaturalApiSettings").Bind(apiSettings);
        services.AddSingleton(apiSettings);

        // Configure HttpClient for auth service
        services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(apiSettings.AuthBaseUrl);
        });

        // Use strongly-typed configuration
        services.AddNaturalApiWithBaseUrl(
            apiSettings.BaseUrl,
            new SimpleCustomAuthProvider(
                new HttpClient { BaseAddress = new Uri(apiSettings.AuthBaseUrl) },
                apiSettings.AuthEndpoint));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IApi>();

        // Act - Use relative endpoint with authentication
        var result = _api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}

public class NaturalApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthBaseUrl { get; set; } = string.Empty;
    public string AuthEndpoint { get; set; } = "/auth/login";
    public string HttpClientName { get; set; } = "NaturalApiClient";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

---

## Testing Without Dependency Injection

This example shows how to use NaturalApi without dependency injection, perfect for simple tests or scripts.

```csharp
[TestClass]
public class NoDiTestingExample
{
    private WireMockServers _wireMockServers = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task Ultra_Simple_Usage_Should_Work()
    {
        // Arrange - Ultra simple usage - no base URL needed
        var api = new Api();

        // Act - Use absolute URL directly
        var result = api.For($"{_wireMockServers.ApiBaseUrl}/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task With_Base_Url_Should_Work()
    {
        // Arrange - Create API with base URL
        var api = new Api(_wireMockServers.ApiBaseUrl);

        // Act - Use relative endpoint
        var result = api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(401, result.StatusCode); // No auth, should be unauthorized
    }

    [TestMethod]
    public async Task With_Base_Url_And_Auth_Should_Work()
    {
        // Arrange - Create API with base URL and auth provider
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) };
        var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        var api = new Api(defaults, new HttpClient { BaseAddress = new Uri(_wireMockServers.ApiBaseUrl) });

        // Act - Use relative endpoint with authentication
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task With_Auth_Provider_Should_Work()
    {
        // Arrange - Create auth provider and API with auth
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.AuthBaseUrl) };
        var authProvider = new SimpleCustomAuthProvider(authHttpClient, "/auth/login");
        var defaults = new DefaultApiDefaults(authProvider: authProvider);
        var httpClient = new HttpClient { BaseAddress = new Uri(_wireMockServers.ApiBaseUrl) };
        var api = new Api(defaults, httpClient);

        // Act - Use relative endpoint with authentication
        var result = api.For("/api/protected").AsUser("testuser", "testpass").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }
}
```

---

## Custom Authentication Service

This example shows how to create a custom authentication service with token caching for production applications.

```csharp
[TestClass]
public class CustomAuthenticationServiceExample
{
    private WireMockServers _wireMockServers = null!;
    private IServiceProvider _serviceProvider = null!;
    private IApi _api = null!;

    [TestInitialize]
    public void Setup()
    {
        _wireMockServers = new WireMockServers();
        
        _serviceProvider = ServiceConfiguration.ConfigureServices(
            authServerUrl: _wireMockServers.AuthBaseUrl,
            apiServerUrl: _wireMockServers.ApiBaseUrl,
            username: "testuser",
            password: "testpass",
            cacheExpiration: TimeSpan.FromMinutes(10));

        _api = _serviceProvider.GetRequiredService<IApi>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _wireMockServers?.Dispose();
    }

    [TestMethod]
    public async Task First_Time_Authentication_Should_Succeed()
    {
        // Act - Make API call (should trigger authentication)
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
    }

    [TestMethod]
    public async Task Cached_Token_Should_Be_Used_On_Subsequent_Calls()
    {
        // Act - Make first call (triggers authentication)
        var result1 = _api.For("/api/protected").Get();
        
        // Make second call (should use cached token)
        var result2 = _api.For("/api/protected").Get();

        // Assert both calls succeed
        Assert.AreEqual(200, result1.StatusCode);
        Assert.AreEqual(200, result2.StatusCode);
    }

    [TestMethod]
    public async Task Auth_Service_Should_Cache_Tokens_Per_User()
    {
        // Arrange - Get auth service directly
        var authService = _serviceProvider.GetRequiredService<IUsernamePasswordAuthService>();

        // Act - Authenticate first time
        var token1 = await authService.AuthenticateAsync("testuser", "testpass");
        
        // Get cached token
        var cachedToken = await authService.GetCachedTokenAsync("testuser");
        
        // Authenticate again (should use cache)
        var token2 = await authService.AuthenticateAsync("testuser", "testpass");

        // Assert
        Assert.IsNotNull(token1);
        Assert.IsNotNull(cachedToken);
        Assert.IsNotNull(token2);
        Assert.AreEqual(token1, cachedToken);
        Assert.AreEqual(token1, token2); // Should be same token from cache
    }

    [TestMethod]
    public async Task End_To_End_Authentication_Flow_Should_Work()
    {
        // This test demonstrates the complete flow:
        // 1. User makes API call
        // 2. NaturalApi detects no auth token
        // 3. Auth provider calls auth service
        // 4. Auth service authenticates with WireMock
        // 5. Token is cached
        // 6. Token is added to request
        // 7. API call succeeds with bearer token

        // Act
        var result = _api.For("/api/protected").Get();

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.RawBody.Contains("Access granted"));
        Assert.IsTrue(result.RawBody.Contains("protected-resource-data"));
    }
}
```

---

## Complete CRUD Operations

### User Management API

```csharp
[TestClass]
public class UserManagementExamples
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _api = new Api("https://api.example.com");
    }

    [TestMethod]
    public async Task Complete_User_CRUD_Operations()
    {
        // CREATE - Create a new user
        var newUser = new
        {
            name = "John Doe",
            email = "john.doe@example.com",
            role = "User"
        };

        var createdUser = await _api.For("/users")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(newUser)
            .ShouldReturn<User>(status: 201, body: u => 
                u.Name == "John Doe" && 
                u.Email == "john.doe@example.com" &&
                u.Id > 0);

        Console.WriteLine($"Created user with ID: {createdUser.Id}");

        // READ - Get the user by ID
        var retrievedUser = await _api.For("/users/{id}")
            .WithPathParam("id", createdUser.Id)
            .Get()
            .ShouldReturn<User>(body: u => u.Id == createdUser.Id);

        Console.WriteLine($"Retrieved user: {retrievedUser.Name}");

        // UPDATE - Update the user
        var updatedUser = new
        {
            id = createdUser.Id,
            name = "John Updated",
            email = "john.updated@example.com",
            role = "Admin"
        };

        var result = await _api.For("/users/{id}")
            .WithPathParam("id", createdUser.Id)
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Put(updatedUser)
            .ShouldReturn<User>(status: 200, body: u => 
                u.Name == "John Updated" && 
                u.Role == "Admin");

        Console.WriteLine($"Updated user: {result.Name}");

        // DELETE - Delete the user
        await _api.For("/users/{id}")
            .WithPathParam("id", createdUser.Id)
            .Delete()
            .ShouldReturn(status: 204);

        Console.WriteLine("User deleted successfully");

        // VERIFY - Confirm user is deleted
        try
        {
            await _api.For("/users/{id}")
                .WithPathParam("id", createdUser.Id)
                .Get()
                .ShouldReturn<User>(status: 200);
            Assert.Fail("User should not exist");
        }
        catch (ApiAssertionException ex) when (ex.ActualStatusCode == 404)
        {
            Console.WriteLine("Confirmed: User no longer exists");
        }
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Authentication Flow

### OAuth 2.0 Authentication Flow

```csharp
[TestClass]
public class AuthenticationFlowExamples
{
    private IApi _api;
    private string _accessToken = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _api = new Api("https://auth.example.com");
    }

    [TestMethod]
    public async Task OAuth2_Authentication_Flow()
    {
        // Step 1: Get authorization code (simulated)
        var authCode = await GetAuthorizationCodeAsync();

        // Step 2: Exchange code for access token
        var tokenResponse = await _api.For("/oauth/token")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            })
            .Post(new
            {
                grant_type = "authorization_code",
                code = authCode,
                client_id = "your-client-id",
                client_secret = "your-client-secret",
                redirect_uri = "https://yourapp.com/callback"
            })
            .ShouldReturn<TokenResponse>(status: 200, body: t => 
                !string.IsNullOrEmpty(t.AccessToken) && 
                t.TokenType == "Bearer");

        _accessToken = tokenResponse.AccessToken;
        Console.WriteLine($"Access token obtained: {_accessToken[..10]}...");

        // Step 3: Use access token to access protected resource
        var userProfile = await _api.For("/api/user/profile")
            .UsingAuth($"Bearer {_accessToken}")
            .Get()
            .ShouldReturn<UserProfile>(status: 200, body: p => 
                !string.IsNullOrEmpty(p.Email));

        Console.WriteLine($"User profile: {userProfile.Name}");

        // Step 4: Refresh token if needed
        if (tokenResponse.ExpiresIn < 300) // Less than 5 minutes
        {
            var refreshedToken = await RefreshAccessTokenAsync(tokenResponse.RefreshToken);
            _accessToken = refreshedToken;
            Console.WriteLine("Token refreshed");
        }
    }

    private async Task<string> GetAuthorizationCodeAsync()
    {
        // Simulate getting authorization code
        return "auth-code-12345";
    }

    private async Task<string> RefreshAccessTokenAsync(string refreshToken)
    {
        var tokenResponse = await _api.For("/oauth/token")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            })
            .Post(new
            {
                grant_type = "refresh_token",
                refresh_token = refreshToken,
                client_id = "your-client-id",
                client_secret = "your-client-secret"
            })
            .ShouldReturn<TokenResponse>(status: 200);

        return tokenResponse.AccessToken;
    }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserProfile
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}
```

### API Key Authentication

```csharp
[TestMethod]
public async Task API_Key_Authentication_Example()
{
    var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "your-api-key";

    var data = await _api.For("/api/data")
        .WithHeader("X-API-Key", apiKey)
        .Get()
        .ShouldReturn<ApiData>(status: 200, body: d => 
            d.Items.Count > 0);

    Console.WriteLine($"Retrieved {data.Items.Count} items");
}
```

---

## Multi-Step Workflows

### E-Commerce Order Processing

```csharp
[TestClass]
public class ECommerceWorkflowExamples
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _api = new Api("https://api.ecommerce.com");
    }

    [TestMethod]
    public async Task Complete_Order_Processing_Workflow()
    {
        // Step 1: Browse products
        var products = await _api.For("/products")
            .WithQueryParams(new
            {
                category = "electronics",
                page = 1,
                limit = 10
            })
            .Get()
            .ShouldReturn<List<Product>>(status: 200, body: p => 
                p.Count > 0 && p.All(pr => pr.Price > 0));

        Console.WriteLine($"Found {products.Count} products");

        // Step 2: Get product details
        var product = await _api.For("/products/{id}")
            .WithPathParam("id", products.First().Id)
            .Get()
            .ShouldReturn<Product>(status: 200, body: p => 
                p.InStock && p.Price > 0);

        Console.WriteLine($"Product: {product.Name} - ${product.Price}");

        // Step 3: Add to cart
        var cartItem = new
        {
            productId = product.Id,
            quantity = 2,
            price = product.Price
        };

        var cart = await _api.For("/cart/items")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(cartItem)
            .ShouldReturn<Cart>(status: 201, body: c => 
                c.Items.Count > 0 && c.Total > 0);

        Console.WriteLine($"Cart total: ${cart.Total}");

        // Step 4: Create order
        var order = new
        {
            items = cart.Items,
            shippingAddress = new
            {
                street = "123 Main St",
                city = "Anytown",
                state = "CA",
                zipCode = "12345"
            },
            paymentMethod = "credit_card"
        };

        var createdOrder = await _api.For("/orders")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(order)
            .ShouldReturn<Order>(status: 201, body: o => 
                o.Id > 0 && o.Status == "Pending");

        Console.WriteLine($"Order created: {createdOrder.Id}");

        // Step 5: Process payment
        var payment = new
        {
            orderId = createdOrder.Id,
            amount = createdOrder.Total,
            cardNumber = "4111111111111111",
            expiryDate = "12/25",
            cvv = "123"
        };

        var paymentResult = await _api.For("/payments")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(payment)
            .ShouldReturn<PaymentResult>(status: 201, body: p => 
                p.Success && p.TransactionId.Length > 0);

        Console.WriteLine($"Payment processed: {paymentResult.TransactionId}");

        // Step 6: Update order status
        var updatedOrder = await _api.For("/orders/{id}")
            .WithPathParam("id", createdOrder.Id)
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Patch(new { status = "Paid" })
            .ShouldReturn<Order>(status: 200, body: o => 
                o.Status == "Paid");

        Console.WriteLine($"Order status updated to: {updatedOrder.Status}");

        // Step 7: Send confirmation email
        var emailRequest = new
        {
            to = "customer@example.com",
            subject = "Order Confirmation",
            template = "order-confirmation",
            data = new { orderId = createdOrder.Id, total = createdOrder.Total }
        };

        await _api.For("/notifications/email")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            })
            .Post(emailRequest)
            .ShouldReturn(status: 202);

        Console.WriteLine("Confirmation email sent");
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class CartItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

---

## Error Handling Scenarios

### Comprehensive Error Handling

```csharp
[TestClass]
public class ErrorHandlingExamples
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _api = new Api("https://api.example.com");
    }

    [TestMethod]
    public async Task Handle_Different_Error_Scenarios()
    {
        // Scenario 1: 404 Not Found
        try
        {
            await _api.For("/users/999")
                .Get()
                .ShouldReturn<User>(status: 200);
            Assert.Fail("Expected 404 error");
        }
        catch (ApiAssertionException ex) when (ex.ActualStatusCode == 404)
        {
            Console.WriteLine("Handled 404: User not found");
            // Log error, show user-friendly message, etc.
        }

        // Scenario 2: 401 Unauthorized
        try
        {
            await _api.For("/protected-resource")
                .Get()
                .ShouldReturn<object>();
            Assert.Fail("Expected 401 error");
        }
        catch (ApiExecutionException ex) when (ex.StatusCode == 401)
        {
            Console.WriteLine("Handled 401: Authentication required");
            // Redirect to login, refresh token, etc.
        }

        // Scenario 3: 500 Server Error with Retry
        var maxRetries = 3;
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                var data = await _api.For("/unreliable-endpoint")
                    .Get()
                    .ShouldReturn<Data>();
                
                Console.WriteLine("Request succeeded after retries");
                return; // Success
            }
            catch (ApiExecutionException ex) when (ex.StatusCode >= 500 && retryCount < maxRetries - 1)
            {
                retryCount++;
                lastException = ex;
                Console.WriteLine($"Server error, retrying ({retryCount}/{maxRetries})...");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // Exponential backoff
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Non-retryable error: {ex.Message}");
                throw;
            }
        }

        // If we get here, all retries failed
        throw lastException ?? new InvalidOperationException("All retry attempts failed");
    }

    [TestMethod]
    public async Task Handle_Timeout_Scenarios()
    {
        try
        {
            var data = await _api.For("/slow-endpoint")
                .WithTimeout(TimeSpan.FromSeconds(5))
                .Get()
                .ShouldReturn<Data>();
        }
        catch (ApiExecutionException ex) when (ex.InnerException is TaskCanceledException)
        {
            Console.WriteLine("Request timed out after 5 seconds");
            // Handle timeout - show user message, log for monitoring, etc.
        }
    }

    [TestMethod]
    public async Task Handle_Validation_Errors()
    {
        try
        {
            var user = await _api.For("/users")
                .Post(new { email = "invalid-email" }) // Invalid email format
                .ShouldReturn<User>(status: 201);
        }
        catch (ApiAssertionException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
            
            // Parse error response for user-friendly messages
            if (!string.IsNullOrEmpty(ex.ResponseBody))
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(ex.ResponseBody);
                    Console.WriteLine($"API Error: {errorResponse.Message}");
                    Console.WriteLine($"Field: {errorResponse.Field}");
                }
                catch (JsonException)
                {
                    Console.WriteLine($"Raw error: {ex.ResponseBody}");
                }
            }
        }
    }
}

public class Data
{
    public string Value { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

---

## Testing Patterns

### Comprehensive Test Suite

```csharp
[TestClass]
public class ComprehensiveTestExamples
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
    public async Task Test_All_HTTP_Methods()
    {
        // GET
        _mockExecutor.SetupResponse(200, """{"id":1,"name":"Test"}""");
        var user = await _api.For("/users/1")
            .Get()
            .ShouldReturn<User>();
        Assert.AreEqual(1, user.Id);

        // POST
        _mockExecutor.SetupResponse(201, """{"id":2,"name":"Created"}""");
        var created = await _api.For("/users")
            .Post(new { name = "New User" })
            .ShouldReturn<User>(status: 201);
        Assert.AreEqual(2, created.Id);

        // PUT
        _mockExecutor.SetupResponse(200, """{"id":1,"name":"Updated"}""");
        var updated = await _api.For("/users/1")
            .Put(new { id = 1, name = "Updated User" })
            .ShouldReturn<User>(status: 200);
        Assert.AreEqual("Updated", updated.Name);

        // PATCH
        _mockExecutor.SetupResponse(200, """{"id":1,"name":"Patched"}""");
        var patched = await _api.For("/users/1")
            .Patch(new { name = "Patched User" })
            .ShouldReturn<User>(status: 200);
        Assert.AreEqual("Patched", patched.Name);

        // DELETE
        _mockExecutor.SetupResponse(204, "");
        await _api.For("/users/1")
            .Delete()
            .ShouldReturn(status: 204);
    }

    [TestMethod]
    public async Task Test_Request_Configuration()
    {
        _mockExecutor.SetupResponse(200, """{"data":"test"}""");

        await _api.For("/test")
            .WithHeader("Accept", "application/json")
            .WithHeader("User-Agent", "TestApp/1.0")
            .WithQueryParam("page", 1)
            .WithQueryParam("limit", 10)
            .WithPathParam("id", 123)
            .WithCookie("session", "abc123")
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Get()
            .ShouldReturn<object>();

        var spec = _mockExecutor.LastSpec;
        Assert.IsTrue(spec.Headers.ContainsKey("Accept"));
        Assert.IsTrue(spec.Headers.ContainsKey("User-Agent"));
        Assert.IsTrue(spec.QueryParams.ContainsKey("page"));
        Assert.IsTrue(spec.QueryParams.ContainsKey("limit"));
        Assert.IsTrue(spec.PathParams.ContainsKey("id"));
        Assert.IsTrue(spec.Cookies.ContainsKey("session"));
        Assert.AreEqual(TimeSpan.FromSeconds(30), spec.Timeout);
    }

    [TestMethod]
    public async Task Test_Authentication_Scenarios()
    {
        // Test with Bearer token
        _mockExecutor.SetupResponse(200, """{"data":"protected"}""");
        await _api.For("/protected")
            .UsingAuth("Bearer token123")
            .Get()
            .ShouldReturn<object>();

        var spec = _mockExecutor.LastSpec;
        Assert.IsTrue(spec.Headers.ContainsKey("Authorization"));
        Assert.AreEqual("Bearer token123", spec.Headers["Authorization"]);

        // Test with API key
        _mockExecutor.SetupResponse(200, """{"data":"api-key-protected"}""");
        await _api.For("/api-protected")
            .WithHeader("X-API-Key", "api-key-123")
            .Get()
            .ShouldReturn<object>();

        spec = _mockExecutor.LastSpec;
        Assert.IsTrue(spec.Headers.ContainsKey("X-API-Key"));
        Assert.AreEqual("api-key-123", spec.Headers["X-API-Key"]);
    }

    [TestMethod]
    public async Task Test_Error_Scenarios()
    {
        // Test 404 error
        _mockExecutor.SetupResponse(404, """{"error":"Not Found"}""");
        try
        {
            await _api.For("/nonexistent")
                .Get()
                .ShouldReturn<object>(status: 200);
            Assert.Fail("Expected 404 error");
        }
        catch (ApiAssertionException ex)
        {
            Assert.AreEqual(404, ex.ActualStatusCode);
        }

        // Test 500 error
        _mockExecutor.SetupResponse(500, """{"error":"Internal Server Error"}""");
        try
        {
            await _api.For("/server-error")
                .Get()
                .ShouldReturn<object>();
            Assert.Fail("Expected 500 error");
        }
        catch (ApiExecutionException ex)
        {
            Assert.AreEqual(500, ex.StatusCode);
        }
    }
}
```

---

## Advanced Configuration

### Multi-Environment Setup

```csharp
public class MultiEnvironmentApiFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MultiEnvironmentApiFactory> _logger;

    public MultiEnvironmentApiFactory(IConfiguration configuration, ILogger<MultiEnvironmentApiFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IApi CreateApi(string environment = null)
    {
        environment ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var baseUrl = environment switch
        {
            "Development" => "https://dev-api.example.com",
            "Staging" => "https://staging-api.example.com",
            "Production" => "https://api.example.com",
            _ => "https://localhost:5001"
        };

        var timeout = environment == "Production" 
            ? TimeSpan.FromSeconds(30) 
            : TimeSpan.FromSeconds(60);

        var defaultHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["User-Agent"] = $"MyApp/1.0 ({environment})"
        };

        var authProvider = environment != "Development" 
            ? new EnvironmentAuthProvider(_configuration)
            : null;

        var defaults = new DefaultApiDefaults(
            baseUri: new Uri(baseUrl),
            defaultHeaders: defaultHeaders,
            timeout: timeout,
            authProvider: authProvider
        );

        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        
        return new Api(executor, defaults);
    }
}

// Usage
var factory = serviceProvider.GetRequiredService<MultiEnvironmentApiFactory>();
var api = factory.CreateApi("Production");
```

### Custom Authentication Provider

```csharp
public class EnvironmentAuthProvider : IApiAuthProvider
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private string? _cachedToken;
    private DateTime _tokenExpiry;

    public EnvironmentAuthProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
    }

    public async Task<string?> GetAuthTokenAsync(string? username = null)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        var clientId = _configuration["Auth:ClientId"];
        var clientSecret = _configuration["Auth:ClientSecret"];
        var tokenEndpoint = _configuration["Auth:TokenEndpoint"];

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            })
        };

        var response = await _httpClient.SendAsync(request);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        
        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 1 minute early
        
        return _cachedToken;
    }
}
```

---

## Integration Examples

### ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add NaturalApi services
builder.Services.AddNaturalApi(options =>
{
    options.RegisterDefaults = true;
});

// Add custom auth provider
builder.Services.AddSingleton<IApiAuthProvider, MyAuthProvider>();

// Add HTTP client
builder.Services.AddHttpClient();

var app = builder.Build();

// Controller
[ApiController]
[Route("api/[controller]")]
public class ExternalApiController : ControllerBase
{
    private readonly IApi _api;

    public ExternalApiController(IApi api)
    {
        _api = api;
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var user = await _api.For($"/users/{id}")
                .Get()
                .ShouldReturn<User>();

            return Ok(user);
        }
        catch (ApiAssertionException ex) when (ex.ActualStatusCode == 404)
        {
            return NotFound($"User {id} not found");
        }
        catch (ApiExecutionException ex)
        {
            return StatusCode(500, $"External API error: {ex.Message}");
        }
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _api.For("/users")
                .WithHeaders(new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                })
                .Post(request)
                .ShouldReturn<User>(status: 201);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (ApiAssertionException ex)
        {
            return BadRequest($"Validation failed: {ex.Message}");
        }
    }
}
```

### Background Service Integration

```csharp
public class DataSyncService : BackgroundService
{
    private readonly IApi _api;
    private readonly ILogger<DataSyncService> _logger;

    public DataSyncService(IApi api, ILogger<DataSyncService> logger)
    {
        _api = api;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncDataAsync();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data sync failed");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task SyncDataAsync()
    {
        var data = await _api.For("/sync/data")
            .WithTimeout(TimeSpan.FromMinutes(10))
            .Get()
            .ShouldReturn<SyncData>();

        _logger.LogInformation("Synced {Count} items", data.Items.Count);
        
        // Process the data...
    }
}
```

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic usage and setup
- **[HTTP Verbs](http-verbs.md)** - Making different types of requests
- **[Request Building](request-building.md)** - Building complex requests
- **[Assertions](assertions.md)** - Validating responses
- **[Authentication](authentication.md)** - Authentication patterns
- **[Testing Guide](testing-guide.md)** - Testing your API calls
- **[Error Handling](error-handling.md)** - Handling errors and exceptions
- **[Configuration](configuration.md)** - Advanced configuration options
- **[Fluent Syntax Reference](fluentsyntax.md)** - Complete method reference
- **[API Reference](api-reference.md)** - Complete interface documentation
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions
