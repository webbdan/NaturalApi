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

### **Default DI Setup**

```csharp
services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
services.AddSingleton<IApiAuthProvider, CachingAuthProvider>();
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