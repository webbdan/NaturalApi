# Simple Authentication Integration Tests

This folder demonstrates the **"just works"** approach to authentication with NaturalApi using per-request credentials.

## Philosophy

The Simple approach prioritizes:
- **Simplicity** - Minimal setup and configuration
- **Per-request credentials** - Pass username/password directly in each request
- **No caching** - Each request authenticates independently
- **Easy to understand** - Straightforward implementation

## Files

- **`SimpleCustomAuthProvider.cs`** - Simple auth provider that accepts credentials per request
- **`SimpleAuthIntegrationTests.cs`** - Integration tests demonstrating the simple approach

## Usage Example

```csharp
// Simple usage - credentials passed per request
var result = api.For("/api/protected")
    .AsUser("username", "password")  // Per-request credentials
    .Get();
```

## When to Use

- **Prototyping** - Quick setup for testing authentication flows
- **Simple applications** - When you don't need token caching
- **Per-request authentication** - When each request uses different credentials
- **Learning** - Easy to understand how NaturalApi authentication works

## Benefits

- ✅ Minimal setup required
- ✅ No complex DI configuration
- ✅ Per-request credential flexibility
- ✅ Easy to understand and debug
- ✅ No token caching complexity

## Trade-offs

- ❌ No token caching (authenticates every request)
- ❌ Less efficient for high-volume scenarios
- ❌ No advanced features like token refresh
