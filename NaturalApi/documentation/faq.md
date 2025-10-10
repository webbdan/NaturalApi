# Frequently Asked Questions

## **1. “So… how is this different from RestAssured / Flurl / Refit / whatever?”**

NaturalApi isn’t a transport library, it’s a **readability and intent layer**.

* RestAssured, Flurl, Refit: designed for developers; their tests read like plumbing instructions.
* NaturalApi: designed for testers; tests read like statements of intent.

You still get full control of the HTTP executor, so you can use `HttpClient`, RestSharp, Playwright, or a mock executor. NaturalApi just builds the request spec and handles the fluent chaining for you.

**Example of a clean, readable test:**

```csharp
var user = await Api.For("/users")
    .WithHeaders("Accept", "application/json")
    .UsingAuth("Bearer myToken")
    .Post(new { name = "Ted" })
    .ShouldReturn<User>(status: 201);
```

Now you have a strongly-typed `user` object and can validate it however you like — unit test asserts, Shouldly, FluentAssertions, whatever:

```csharp
user.Id.ShouldNotBeNull();          // Shouldly
user.Name.Should().Be("Ted");       // FluentAssertions
Assert.Contains("Tester", user.Roles); // xUnit
```

---

## **2. “Can I still validate complex response structures, not just status codes?”**

Absolutely, and this is the **preferred pattern**. Use `ShouldReturn<T>()` to get the typed response, then do all the assertions separately.

```csharp
var user = await Api.For("/users")
    .Post(new { name = "Ted" })
    .ShouldReturn<User>(status: 201);

// separate, clear validation
user.Id.ShouldNotBeNull();
user.Name.ShouldBe("Ted");
user.Roles.ShouldContain("Tester");
```

Benefits:

* Clear separation of execution vs validation
* Strongly typed models
* Can mix any assertion framework — NaturalApi doesn’t care

You’re no longer forced to stuff all your validations inside anonymous lambdas or chain weird `.Then()` calls just to get access to the response.

---

## **3. “What happens when something fails or the request blows up?”**

NaturalApi wraps all transport-layer exceptions in a clear, consistent `ApiExecutionException`.

You get:

* Method
* Endpoint
* Headers
* Request body
* Inner exception

Example output when a POST fails:

```
ApiExecutionException: POST /users failed
→ Reason: Connection refused
→ Inner: HttpRequestException
→ Body: {"name":"Ted"}
```

This gives your testers **context first, stack trace second**.

And once the request succeeds, you can still pull the response object out via `ShouldReturn<T>()` and validate however you want, with **any assertion framework**:

```csharp
var user = await Api.For("/users").Get().ShouldReturn<User>(status: 200);

user.Id.Should().BePositive();       // FluentAssertions
user.Name.ShouldNotBeNullOrEmpty();  // Shouldly
Assert.Equal("Ted", user.Name);      // xUnit
```

---

## **4. “Can I validate simple response structures?”**

The `.ShouldReturn()` block is expressive enough for that.
You can assert on:

* status code
* headers
* content type
* and full typed models

Example:

```csharp
await Api.For("/users")
    .Post(new { name = "Ted" })
    .ShouldReturn(r => r
        .StatusCode(201)
        .Body<User>(u =>
        {
            u.Id.ShouldNotBeNull();
            u.Name.ShouldBe("Ted");
            u.Roles.ShouldContain("Tester");
        })
    );
```

The philosophy: you write assertions in *your test language*, not by bending to a DSL’s rules.



