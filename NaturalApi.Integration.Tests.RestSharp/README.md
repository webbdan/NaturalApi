# NaturalApi RestSharp Integration Tests

This project demonstrates and tests the extensibility of NaturalApi with RestSharp as a custom HTTP executor.

## Overview

This test project proves that NaturalApi can be extended to work with alternative HTTP clients like RestSharp, while maintaining the same fluent API and all existing functionality.

## Test Structure

- **Executors/**: Contains RestSharp-specific implementations
  - `RestSharpHttpExecutor.cs`: RestSharp implementation of `IHttpExecutor`
  - `RestSharpApiResultContext.cs`: RestSharp implementation of `IApiResultContext`

- **Tests/**: Integration tests proving functionality
  - `RestSharpRegistrationTests.cs`: Tests generic executor registration
  - `RestSharpBasicTests.cs`: Tests all HTTP operations (GET, POST, PUT, PATCH, DELETE)
  - `RestSharpAuthTests.cs`: Tests authentication with custom executors

- **Common/**: Test helpers and utilities
  - `RestSharpTestHelpers.cs`: Helper methods for test setup
  - `WireMockServers.cs`: WireMock server for HTTP mocking

## Key Features Tested

### 1. Generic Executor Registration
```csharp
// Simple registration
services.AddNaturalApi<RestSharpHttpExecutor>();

// Factory-based registration
services.AddNaturalApi<RestSharpHttpExecutor>(provider => 
    new RestSharpHttpExecutor("https://api.example.com"));

// Options-based registration
services.AddNaturalApi<RestSharpHttpExecutor, RestSharpOptions>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

### 2. Full HTTP Operations Support
- GET, POST, PUT, PATCH, DELETE requests
- Headers, query parameters, path parameters
- Request bodies and cookies
- Timeout handling
- Response deserialization

### 3. Authentication Support
- Bearer token authentication
- Custom authentication schemes
- Per-user authentication contexts
- Auth provider integration

### 4. Fluent API Compatibility
All NaturalApi fluent methods work identically with RestSharp:
```csharp
var result = api.For("/users")
    .WithHeader("Accept", "application/json")
    .WithQueryParam("page", 1)
    .UsingToken("bearer-token")
    .Get()
    .ShouldBeSuccessful()
    .And()
    .ShouldHaveStatusCode(200);
```

## Running the Tests

```bash
dotnet test NaturalApi.Integration.Tests.RestSharp
```

## Dependencies

- **RestSharp**: HTTP client library
- **WireMock.Net**: HTTP server mocking
- **MSTest**: Testing framework
- **NaturalApi**: Core library being tested

## Proving Extensibility

This test project demonstrates that NaturalApi is truly extensible by:

1. **Registration**: Generic `AddNaturalApi<TExecutor>()` works with RestSharp
2. **Functionality**: All HTTP operations work identically to HttpClient
3. **Authentication**: Auth providers work seamlessly with custom executors
4. **Compatibility**: Same fluent API, same validation methods, same behavior
5. **No Breaking Changes**: Existing HttpClient-based code continues to work

## Usage Example

```csharp
// Register RestSharp executor
services.AddNaturalApi<RestSharpHttpExecutor>(provider => 
    new RestSharpHttpExecutor("https://api.example.com"));

// Use exactly like HttpClient-based NaturalApi
var api = serviceProvider.GetRequiredService<IApi>();
var result = await api.For("/users").Get().ShouldBeSuccessful();
```

This proves that NaturalApi's extensibility design allows users to plug in any HTTP client library while maintaining the same developer experience.
