## **Fluent Syntax Overview**

This section defines the structure, expected flow, and conventions of the fluent API. It is a living grammar for how testers describe API calls in code.

---

### **1. Core Grammar**

Every request follows a predictable sentence structure:

```
Api.For(endpoint)
   .WithHeaders(...)
   .WithQueryParams(...)
   .WithPathParams(...)
   .UsingAuth(...)
   .<HttpVerb>(body)
   .ShouldReturn<T>(...)
```

Each segment builds intent; nothing is syntactic filler.
You can omit anything not relevant — the grammar remains valid.

---

### **2. Core Methods**

| Method                                                  | Description                                         | Returns       |
| ------------------------------------------------------- | --------------------------------------------------- | ------------- |
| `Api.For(string endpoint)`                              | Defines the target endpoint (absolute or relative). | `IApiContext` |
| `.WithHeader(string key, string value)`                 | Adds a single HTTP header.                          | `IApiContext` |
| `.WithHeaders(Dictionary<string,string> headers)`       | Adds multiple headers.                              | `IApiContext` |
| `.WithQueryParam(string key, object value)`             | Adds one query parameter.                           | `IApiContext` |
| `.WithQueryParams(object or Dictionary<string,object>)` | Adds multiple query parameters.                     | `IApiContext` |
| `.WithPathParam(string key, object value)`              | Replaces `{key}` in the path.                       | `IApiContext` |
| `.WithPathParams(object or Dictionary<string,object>)`  | Replaces multiple path parameters.                  | `IApiContext` |
| `.WithCookie(string name, string value)`                | Adds a single cookie.                               | `IApiContext` |
| `.WithCookies(Dictionary<string,string> cookies)`        | Adds multiple cookies.                              | `IApiContext` |
| `.ClearCookies()`                                        | Removes all cookies from the request.               | `IApiContext` |
| `.WithTimeout(TimeSpan timeout)`                        | Sets request timeout.                               | `IApiContext` |
| `.UsingAuth(string schemeOrToken)`                      | Adds an authentication header (Bearer, Basic, etc). | `IApiContext` |
| `.UsingToken(string token)`                             | Shortcut for Bearer tokens.                         | `IApiContext` |
| `.WithoutAuth()`                                         | Disables authentication for this request.            | `IApiContext` |
| `.AsUser(string username)`                               | Sets user context for per-user authentication.     | `IApiContext` |

---

### **3. Cookie Methods**

NaturalApi supports cookie management for session-based authentication and state management.

| Method                                          | Description                           | Returns       |
| ----------------------------------------------- | ------------------------------------- | ------------- |
| `.WithCookie(string name, string value)`       | Adds a single cookie to the request.  | `IApiContext` |
| `.WithCookies(Dictionary<string,string> cookies)` | Adds multiple cookies to the request. | `IApiContext` |
| `.ClearCookies()`                               | Removes all cookies from the request. | `IApiContext` |

**Examples:**
```csharp
// Single cookie
var data = await api.For("/protected")
    .WithCookie("session", "abc123")
    .Get()
    .ShouldReturn<Data>();

// Multiple cookies
var data = await api.For("/dashboard")
    .WithCookies(new Dictionary<string, string>
    {
        ["session"] = "abc123",
        ["theme"] = "dark",
        ["language"] = "en"
    })
    .Get()
    .ShouldReturn<Data>();

// Clear cookies
var result = await api.For("/logout")
    .ClearCookies()
    .Post()
    .ShouldReturn<LogoutResult>();
```

---

### **4. HTTP Verbs**

Each verb executes the request and transitions into a result context.

| Verb                  | Signature                       | Returns             |
| --------------------- | ------------------------------- | ------------------- |
| `.Get()`              | Sends GET request.              | `IApiResultContext` |
| `.Post(object body)`  | Sends POST with optional body.  | `IApiResultContext` |
| `.Put(object body)`   | Sends PUT with optional body.   | `IApiResultContext` |
| `.Patch(object body)` | Sends PATCH with optional body. | `IApiResultContext` |
| `.Delete()`           | Sends DELETE request.           | `IApiResultContext` |

All verbs support both synchronous and asynchronous use:

```csharp
await Api.For("/users").Get();
```

Async support is automatic; testers never deal with `Task<T>` directly.

---

### **5. Assertions and Validation**

The validation step always begins with `.ShouldReturn()`, which transitions into an expectation context.

#### **Overloads**

```csharp
.ShouldReturn<T>()                            // Type validation only
.ShouldReturn(status: int)                    // Status validation
.ShouldReturn<T>(status: int)                 // Type and status
.ShouldReturn(Action<T> validator)            // Inline body validation
.ShouldReturn<T>(status: int, Action<T> validator)
.ShouldReturn(status: int, headers: Func<IDictionary<string,string>,bool>)
```

#### **Examples**

```csharp
Api.For("/users").Get().ShouldReturn<List<User>>(status: 200);

Api.For("/orders")
   .Post(newOrder)
   .ShouldReturn<OrderCreated>(
       status: 201,
       body => body.Id > 0 && body.Total > 0
   );

Api.For("/health")
   .Get()
   .ShouldReturn(status: 200, headers: h => h.ContainsKey("X-Version"));
```

---

### **6. Optional Enhancements**

| Method                           | Description                                         |
| -------------------------------- | --------------------------------------------------- |
| `.Expect()`                      | Alias for `.ShouldReturn()` (stylistic preference). |
| `.Then(Action<IApiResult>)`      | Allows follow-up logic or chained requests.         |
| `.WithTimeout(TimeSpan timeout)` | Optional request-level timeout override.            |
| `.WithBaseUrl(string baseUrl)`   | Allows dynamic context-based endpoint resolution.   |
| `.LogTo(IReporter reporter)`     | Hooks in custom logging (console, file, telemetry). |

---

### **7. Result Context**

After execution, `IApiResultContext` exposes the following:

| Member         | Type                         | Description                  |
| -------------- | ---------------------------- | ---------------------------- |
| `.Response`    | `HttpResponseMessage`        | Raw HTTP response.           |
| `.StatusCode`  | `int`                        | Numeric status code.         |
| `.Headers`     | `IDictionary<string,string>` | Flattened headers.           |
| `.BodyAs<T>()` | `T`                          | Deserialises body into type. |
| `.RawBody`     | `string`                     | Raw response body.           |

All members are safe to call; no hidden streams or disposal surprises.

---

### **8. Example — Full Flow**

```csharp
Api.For("/users/{id}")
   .WithPathParam("id", 42)
   .WithHeaders(new { Accept = "application/json" })
   .UsingAuth("Bearer xyz123")
   .Get()
   .ShouldReturn<User>(
       status: 200,
       body => body.Id == 42 && body.Name == "Dan"
   );
```

Reads like English, executes like code, fails like truth.

---

### **9. Example — Chained Continuation**

```csharp
Api.For("/users")
   .Post(newUser)
   .ShouldReturn<UserCreated>(status: 201)
   .Then(result => 
        Api.For($"/users/{result.Body.Id}")
           .Get()
           .ShouldReturn<User>(body => body.Email == newUser.Email)
   );
```

Declarative, legible, and free from setup clutter.

---

### **10. Grammar Rules Summary**

| Rule                                                   | Description |
| ------------------------------------------------------ | ----------- |
| Start with `Api.For()`                                 |             |
| Build context using `.With*()` or `.Using*()`          |             |
| Execute using HTTP verb                                |             |
| Assert using `.ShouldReturn()`                         |             |
| Optionally chain with `.Then()`                        |             |
| All steps are optional except `For()` and an HTTP verb |             |

---

## Related Topics

- **[Getting Started](getting-started.md)** - Basic usage and setup
- **[Request Building](request-building.md)** - Detailed request building guide
- **[HTTP Verbs](http-verbs.md)** - Complete HTTP verb usage
- **[Assertions](assertions.md)** - Response validation patterns
- **[Authentication](authentication.md)** - Authentication methods
- **[Examples](examples.md)** - Real-world usage scenarios
- **[API Reference](api-reference.md)** - Complete interface documentation
- **[Testing Guide](testing-guide.md)** - Testing with NaturalApi
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions

