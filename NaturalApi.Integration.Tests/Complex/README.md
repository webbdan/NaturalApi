# Complex Authentication Integration Tests

This folder demonstrates the **advanced** approach to authentication with NaturalApi using token caching, dependency injection, and enterprise-grade patterns.

## Philosophy

The Complex approach prioritizes:
- **Performance** - Token caching to avoid repeated authentication
- **Scalability** - Proper DI configuration for production use
- **Enterprise patterns** - Separation of concerns and testability
- **Advanced features** - Token expiration, refresh, and multi-user support

## Files

- **`CustomAuthProvider.cs`** - Advanced auth provider with token caching
- **`UsernamePasswordAuthService.cs`** - Authentication service with caching logic
- **`ServiceConfiguration.cs`** - DI configuration for complex scenarios
- **`AuthenticationIntegrationTests.cs`** - Comprehensive integration tests

## Usage Example

```csharp
// Complex usage - configured with DI and caching
var services = new ServiceCollection();
services.AddHttpClient<IUsernamePasswordAuthService, UsernamePasswordAuthService>();
services.AddSingleton<IApiAuthProvider, CustomAuthProvider>();
services.AddNaturalApi();

var api = services.BuildServiceProvider().GetRequiredService<IApi>();

// Uses cached tokens automatically
var result = api.For("/api/protected").Get();
```

## When to Use

- **Production applications** - When you need performance and reliability
- **High-volume scenarios** - When token caching is essential
- **Enterprise applications** - When you need proper DI and separation of concerns
- **Multi-user systems** - When you need per-user token management

## Benefits

- ✅ Token caching for performance
- ✅ Proper DI configuration
- ✅ Enterprise-grade patterns
- ✅ Multi-user token management
- ✅ Token expiration handling
- ✅ Comprehensive test coverage

## Trade-offs

- ❌ More complex setup
- ❌ Requires understanding of DI patterns
- ❌ More files and configuration
- ❌ Harder to understand for beginners
