# NaturalApi Playwright Integration Tests

This project demonstrates and tests the extensibility of NaturalApi with Playwright's APIRequestContext as a custom HTTP executor.

## Overview

This test project proves that NaturalApi can be extended to work with Playwright's APIRequestContext, while maintaining the same fluent API and all existing functionality. This demonstrates NaturalApi's flexibility in supporting different HTTP client libraries.

## Test Structure

- **Executors/**: Contains Playwright-specific implementations
  - `PlaywrightHttpExecutor.cs`: Playwright implementation of `IHttpExecutor`
  - `PlaywrightApiResultContext.cs`: Playwright implementation of `IApiResultContext`

- **Tests/**: Integration tests proving functionality
  - `PlaywrightRegistrationTests.cs`: Tests generic executor registration
  - `PlaywrightBasicTests.cs`: Tests all HTTP operations (GET, POST, PUT, PATCH, DELETE)
  - `PlaywrightAuthTests.cs`: Tests authentication with custom executors

- **Common/**: Test helpers and utilities
  - `PlaywrightTestHelpers.cs`: Helper methods for test setup
  - `WireMockServers.cs`: WireMock server for HTTP mocking

## Key Features Tested

### 1. Generic Executor Registration
```csharp
// Simple registration
services.AddNaturalApi<PlaywrightHttpExecutor>();

// Factory-based registration
services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
    new PlaywrightHttpExecutor("https://api.example.com"));

// Options-based registration
services.AddNaturalApi<PlaywrightHttpExecutor, PlaywrightOptions>(options =>
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
All NaturalApi fluent methods work identically with Playwright:
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
dotnet test NaturalApi.Integration.Tests.Playwright
```

## Dependencies

- **Microsoft.Playwright**: Browser automation and API testing library
- **WireMock.Net**: HTTP server mocking
- **MSTest**: Testing framework
- **NaturalApi**: Core library being tested

## Proving Extensibility

This test project demonstrates that NaturalApi is truly extensible by:

1. **Registration**: Generic `AddNaturalApi<TExecutor>()` works with Playwright
2. **Functionality**: All HTTP operations work identically to HttpClient
3. **Authentication**: Auth providers work seamlessly with custom executors
4. **Compatibility**: Same fluent API, same validation methods, same behavior
5. **No Breaking Changes**: Existing HttpClient-based code continues to work

## Usage Example

```csharp
// Register Playwright executor
services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
    new PlaywrightHttpExecutor("https://api.example.com"));

// Use exactly like HttpClient-based NaturalApi
var api = serviceProvider.GetRequiredService<IApi>();
var result = await api.For("/users").Get().ShouldBeSuccessful();
```

## Playwright-Specific Features

### APIRequestContext Integration
- Uses Playwright's `IAPIRequestContext` for HTTP operations
- Leverages Playwright's robust request/response handling
- Maintains compatibility with Playwright's async nature

### Error Handling
- Wraps Playwright exceptions in `ApiExecutionException`
- Maintains consistency with NaturalApi error handling patterns
- Provides clear error messages for debugging

### Response Processing
- Extracts status codes, headers, and body from Playwright responses
- Supports JSON deserialization with case-insensitive property matching
- Implements cookie extraction from Set-Cookie headers

This proves that NaturalApi's extensibility design allows users to plug in any HTTP client library while maintaining the same developer experience.
