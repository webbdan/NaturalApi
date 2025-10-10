# Contributing Guide

> This guide covers the architecture internals, design principles, and contribution process for NaturalApi.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Design Principles](#design-principles)
- [Code Standards](#code-standards)
- [Development Workflow](#development-workflow)
- [Testing Requirements](#testing-requirements)
- [Documentation Standards](#documentation-standards)
- [AI Modification Policy](#ai-modification-policy)
- [Pull Request Process](#pull-request-process)

---

## Architecture Overview

NaturalApi is built on four clean layers, each with a single responsibility:

### 1. Fluent DSL Layer
- **Purpose**: User-facing grammar that reads like natural language
- **Key Components**: `IApi`, `IApiContext`, `IApiResultContext`
- **Design**: Immutable context objects that compose new contexts fluently

### 2. Context Builders Layer
- **Purpose**: Immutable state composition and request building
- **Key Components**: `ApiContext`, `ApiRequestSpec`
- **Design**: Each method returns a new context with accumulated state

### 3. Execution Engine Layer
- **Purpose**: HTTP handling and response processing
- **Key Components**: `IHttpExecutor`, `HttpClientExecutor`, `AuthenticatedHttpClientExecutor`
- **Design**: Pluggable executors for different HTTP scenarios

### 4. Validation Layer
- **Purpose**: Declarative assertions with readable error messages
- **Key Components**: `IApiValidator`, `ApiAssertionException`
- **Design**: Decoupled from DSL and HTTP handling

### Layer Communication

```
Fluent DSL
    ↓ (creates contexts)
Context Builders
    ↓ (accumulates state)
Execution Engine
    ↓ (processes HTTP)
Validation Layer
    ↓ (validates response)
User Code
```

---

## Design Principles

### 1. Immutability First

All context objects are immutable. Each method returns a new context with accumulated state:

```csharp
// Good - immutable
public IApiContext WithHeader(string key, string value)
{
    var newSpec = _spec.WithHeader(key, value);
    return new ApiContext(newSpec, _executor, _authProvider);
}

// Avoid - mutable
public IApiContext WithHeader(string key, string value)
{
    _spec.Headers[key] = value; // DON'T DO THIS
    return this;
}
```

### 2. Single Responsibility

Each class has one reason to change:

```csharp
// Good - single responsibility
public class ApiContext : IApiContext
{
    // Only handles context building
}

public class HttpClientExecutor : IHttpExecutor
{
    // Only handles HTTP execution
}

// Avoid - multiple responsibilities
public class ApiContext : IApiContext, IHttpExecutor
{
    // DON'T DO THIS - mixing concerns
}
```

### 3. Interface Segregation

Interfaces should be focused and cohesive:

```csharp
// Good - focused interface
public interface IApiContext
{
    IApiContext WithHeader(string key, string value);
    IApiResultContext Get();
}

// Avoid - bloated interface
public interface IApiContext
{
    IApiContext WithHeader(string key, string value);
    IApiResultContext Get();
    void LogTo(ILogger logger); // DON'T MIX CONCERNS
    void CacheFor(TimeSpan duration); // DON'T MIX CONCERNS
}
```

### 4. Dependency Inversion

Depend on abstractions, not concretions:

```csharp
// Good - depends on abstraction
public class Api
{
    private readonly IHttpExecutor _executor;
    
    public Api(IHttpExecutor executor)
    {
        _executor = executor;
    }
}

// Avoid - depends on concrete class
public class Api
{
    private readonly HttpClientExecutor _executor;
    
    public Api(HttpClientExecutor executor) // DON'T DO THIS
    {
        _executor = executor;
    }
}
```

---

## Code Standards

### 1. Clean Code Principles

Follow the established clean code principles:

- **Meaningful Names**: Use descriptive and specific names
- **Small Functions**: Functions should do one thing only
- **Single Responsibility**: One reason to change
- **DRY**: Don't repeat yourself
- **Encapsulation**: Hide implementation details

### 2. Naming Conventions

```csharp
// Good - descriptive names
public IApiContext WithQueryParam(string key, object value)
public IApiResultContext ShouldReturn<T>(int? status = null)
public class AuthenticatedHttpClientExecutor

// Avoid - unclear names
public IApiContext WithQP(string k, object v) // DON'T ABBREVIATE
public IApiResultContext SR<T>(int? s = null) // DON'T ABBREVIATE
public class AuthHttpExec // DON'T ABBREVIATE
```

### 3. Method Signatures

Keep method signatures simple and focused:

```csharp
// Good - focused parameters
public IApiContext WithHeader(string key, string value)
public IApiResultContext ShouldReturn<T>(int? status = null, Action<T>? bodyValidator = null)

// Avoid - too many parameters
public IApiResultContext ShouldReturn<T>(
    int? status = null,
    Action<T>? bodyValidator = null,
    Func<IDictionary<string, string>, bool>? headers = null,
    TimeSpan? timeout = null,
    bool? cache = null) // DON'T OVERLOAD
```

### 4. Error Handling

Use appropriate exception types:

```csharp
// Good - specific exceptions
public void ValidateStatus(HttpResponseMessage response, int expected)
{
    if ((int)response.StatusCode != expected)
    {
        throw new ApiAssertionException($"Expected status {expected} but got {(int)response.StatusCode}");
    }
}

// Avoid - generic exceptions
public void ValidateStatus(HttpResponseMessage response, int expected)
{
    if ((int)response.StatusCode != expected)
    {
        throw new Exception("Status code mismatch"); // DON'T USE GENERIC EXCEPTIONS
    }
}
```

---

## Development Workflow

### 1. Fork and Clone

```bash
git clone https://github.com/your-username/NaturalApi.git
cd NaturalApi
```

### 2. Create Feature Branch

```bash
git checkout -b feature/your-feature-name
```

### 3. Make Changes

Follow the established patterns and principles.

### 4. Run Tests

```bash
dotnet test
```

### 5. Update Documentation

Update relevant documentation files if your changes affect the public API.

### 6. Commit Changes

```bash
git add .
git commit -m "feat: add new feature description"
```

### 7. Push and Create PR

```bash
git push origin feature/your-feature-name
```

---

## Testing Requirements

### 1. Unit Tests

Every public method must have unit tests:

```csharp
[TestMethod]
public void WithHeader_Should_Add_Header_To_Spec()
{
    // Arrange
    var spec = new ApiRequestSpec("/test", HttpMethod.Get, new Dictionary<string, string>(), new Dictionary<string, object>(), new Dictionary<string, object>(), null, null);
    var context = new ApiContext(spec, _mockExecutor);

    // Act
    var result = context.WithHeader("Accept", "application/json");

    // Assert
    Assert.IsTrue(result._spec.Headers.ContainsKey("Accept"));
    Assert.AreEqual("application/json", result._spec.Headers["Accept"]);
}
```

### 2. Integration Tests

Test real HTTP scenarios:

```csharp
[TestMethod]
public async Task Get_User_Should_Return_User_From_Real_API()
{
    // Arrange
    var api = new Api("https://jsonplaceholder.typicode.com");

    // Act
    var user = await api.For("/users/1")
        .Get()
        .ShouldReturn<User>();

    // Assert
    Assert.IsNotNull(user);
    Assert.AreEqual(1, user.Id);
}
```

### 3. Test Coverage

Maintain high test coverage for all public APIs.

### 4. Test Categories

Use test categories appropriately:

```csharp
[TestMethod]
[TestCategory("Unit")]
public void Unit_Test_Method() { }

[TestMethod]
[TestCategory("Integration")]
public void Integration_Test_Method() { }
```

---

## Documentation Standards

### 1. XML Documentation

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Adds a single HTTP header to the request.
/// </summary>
/// <param name="key">Header name</param>
/// <param name="value">Header value</param>
/// <returns>New context with the header added</returns>
public IApiContext WithHeader(string key, string value)
```

### 2. Code Examples

Include usage examples in documentation:

```csharp
/// <summary>
/// Creates a new API context for the specified endpoint.
/// </summary>
/// <param name="endpoint">The target endpoint (absolute or relative URL)</param>
/// <returns>An API context for building and executing requests</returns>
/// <example>
/// <code>
/// var api = new Api("https://api.example.com");
/// var context = api.For("/users");
/// </code>
/// </example>
public IApiContext For(string endpoint)
```

### 3. Update Documentation

When adding new features, update:
- README.md
- Relevant guide files
- API reference
- Examples

---

## AI Modification Policy

### 1. AI Modification Comments

All AI-modified files must include the AI modification comment:

```csharp
// AIModified:2025-10-10T08:03:28Z
namespace NaturalApi;

public class MyClass
{
    // Implementation
}
```

### 2. Comment Format

Use the exact format: `// AIModified:YYYY-MM-DDTHH:MM:SSZ`

### 3. Timestamp Requirements

- Use UTC timezone (Z suffix)
- Include full date and time
- Use ISO 8601 format

### 4. File Protection

Files with `// NoAI` comments cannot be modified without explicit permission:

```csharp
// NoAI
namespace NaturalApi;

public class ProtectedClass
{
    // This file is protected from AI modifications
}
```

### 5. Updating Timestamps

When modifying an AI-touched file, update the existing timestamp:

```csharp
// AIModified:2025-10-10T10:30:00Z  // Updated timestamp
namespace NaturalApi;

public class MyClass
{
    // Updated implementation
}
```

---

## Pull Request Process

### 1. PR Requirements

- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] No breaking changes (or properly documented)
- [ ] AI modification comments are present (if applicable)

### 2. PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] All tests pass

## Documentation
- [ ] README updated
- [ ] API reference updated
- [ ] Guide files updated
```

### 3. Review Process

1. **Automated Checks**: CI/CD pipeline runs tests and checks
2. **Code Review**: Maintainer reviews code quality and design
3. **Documentation Review**: Ensures documentation is complete
4. **Approval**: Maintainer approves and merges

### 4. Breaking Changes

Breaking changes require:
- Major version bump
- Migration guide
- Deprecation notices
- Extended review process

---

## Architecture Internals

### 1. Context Immutability

Contexts are immutable to ensure thread safety and predictable behavior:

```csharp
public IApiContext WithHeader(string key, string value)
{
    // Create new spec with added header
    var newSpec = _spec.WithHeader(key, value);
    
    // Return new context with new spec
    return new ApiContext(newSpec, _executor, _authProvider);
}
```

### 2. Request Specification

`ApiRequestSpec` is a record that stores all request state:

```csharp
public record ApiRequestSpec
{
    public string Endpoint { get; init; }
    public HttpMethod Method { get; init; }
    public IDictionary<string, string> Headers { get; init; }
    // ... other properties
}
```

### 3. Execution Flow

1. **Context Building**: User chains methods to build request spec
2. **Specification**: `ApiRequestSpec` contains all request details
3. **Execution**: `IHttpExecutor` processes the request
4. **Result**: `IApiResultContext` provides response and validation

### 4. Validation Separation

Validation is separated from execution to allow for different validation strategies:

```csharp
public interface IApiValidator
{
    void ValidateStatus(HttpResponseMessage response, int expected);
    void ValidateHeaders(HttpResponseMessage response, Func<IDictionary<string, string>, bool> predicate);
    void ValidateBody<T>(string rawBody, Action<T> validator);
}
```

---

## Best Practices for Contributors

### 1. Start Small

Begin with small, focused changes:
- Fix typos in documentation
- Add missing tests
- Improve error messages

### 2. Follow Existing Patterns

Study existing code before making changes:
- How are similar features implemented?
- What patterns are used for error handling?
- How are tests structured?

### 3. Ask Questions

Don't hesitate to ask questions:
- Open an issue for discussion
- Ask in pull request comments
- Reach out to maintainers

### 4. Test Your Changes

Always test your changes:
- Run existing tests
- Add new tests for new features
- Test edge cases

### 5. Document Your Changes

Update documentation for any public API changes:
- Add XML documentation
- Update examples
- Update guides if needed

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic concepts for contributors
- **[Architecture Overview](architectureanddesign.md)** - Detailed architecture documentation
- **[API Reference](api-reference.md)** - Complete API documentation
- **[Extensibility Guide](extensibility.md)** - Creating custom implementations
- **[Testing Guide](testing-guide.md)** - Testing patterns and best practices
- **[Examples](examples.md)** - Real-world usage scenarios
- **[Philosophy & Design Principles](philosophyanddesignprinciples.md)** - Core design philosophy
- **[Fluent Syntax Reference](fluentsyntax.md)** - Complete method reference
- **[Dependency Injection Guide](di.md)** - DI patterns and ServiceCollectionExtensions
- **[Configuration](configuration.md)** - Setup and configuration patterns
