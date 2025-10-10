## **Philosophy and Design Principles**

This library exists to make API testing as natural and readable as describing it aloud. It is not a BDD toybox, not a wrapper for `HttpClient`, and not an exercise in ceremony. It is a domain-specific language that speaks in the voice of testers who want to *think about behaviour, not boilerplate.*

---

### **1. Speak Like a Human**

The DSL should read like a sentence. Each chained call contributes meaning, not syntax. If you can read it out loud and it makes sense, it’s correct.

Example:
`Api.For("/users").WithHeaders(...).UsingAuth(...).Post(body).ShouldReturn<CreatedUser>()`

Developers understand code; testers understand intent. The design bridges both.

---

### **2. Flow Mirrors Tester Logic**

The chaining order follows how testers reason about a request:

> “For this endpoint, with these details, using this setup, perform this call, and it should return this.”

This translates directly into:
`For → With → Using → Do → ShouldReturn`

Each step reads naturally and feels consistent across verbs (GET, POST, PUT, DELETE).

---

### **3. Optional, Predictable, Composable**

Every method in the chain is optional, and the result must always be coherent.
Users can stop at any stage without invalidating the mental model.

Examples:

* Minimal: `Api.For("/users").Get().ShouldReturn<Users>()`
* Full: `Api.For("/users").WithHeaders(...).WithQueryParams(...).UsingAuth(...).Post(body).ShouldReturn(status:201)`

---

### **4. Fluent, Not Fragile**

Fluency means you never break the chain.
Each segment returns a context-aware object that naturally leads to the next logical action.
There is no `.Build()`, `.Execute()`, or hidden setup step. The “call” is the moment of action.

---

### **5. `ShouldReturn` Is the Assertion Core**

Assertions live in the `ShouldReturn` step.
This is where testers describe expectations declaratively, not procedurally.

Forms may include:

* **Type expectations:** `ShouldReturn<User>()`
* **Status validation:** `ShouldReturn(status: 200)`
* **Body validation:** `ShouldReturn(body => body.Name == "Dan")`
* **Compound:** `ShouldReturn<User>(status:200, body => body.Id > 0)`

This isolates behavioural checking from request definition, avoiding tangled logic.

---

### **6. Extensions Follow Natural Language**

Singular for one, plural for many. Predictable consistency builds trust.

| Concept         | Singular            | Plural               |
| --------------- | ------------------- | -------------------- |
| Header          | `.WithHeader()`     | `.WithHeaders()`     |
| Query Parameter | `.WithQueryParam()` | `.WithQueryParams()` |
| Path Parameter  | `.WithPathParam()`  | `.WithPathParams()`  |

Future syntax extensions follow the same convention:

* `.UsingToken("...")` — explicit auth shortcut
* `.Expect()` — alias for `ShouldReturn`
* `.Then()` — optional continuation for chained validations or subsequent calls

---

### **7. Swagger and VSIX Generation**

When the user right-clicks a Swagger document and selects “Generate API Test”, the resulting code should look *exactly like something a human would write*.

Example generated chain:
`Api.For("/orders/{id}").WithPathParam("id", 123).UsingAuth("Bearer ...").Get().ShouldReturn<Order>()`

Generation should follow this grammar precisely — no private framework voodoo.

---

### **8. Simplicity over Asynchrony**

Asynchronous behaviour is handled transparently.
The tester should never be forced to understand or manage `Task<T>` lifecycles.
`await` should *just work*, and synchronous execution should not punish them either.

---

### **9. Failure Messages Should Read Like Sentences**

When things go wrong, output must be readable by humans.

Example:

> Expected status 201 but got 500 for POST /users.
> Expected body.Name = "Dan", actual = "Ian".

Readable errors turn debugging from archaeology into conversation.

---

### **10. No Fake BDD**

We do not pretend to be “Given/When/Then”.
Real BDD lives in conversations, not syntax. This library exists to express clear, structured intent in code, not to re-enact Cucumber theatre.

---

### **11. Principle of Minimum Magic**

Hidden complexity is tolerated only when it removes *repetition*, never when it hides *meaning*.
Users should always be able to reason through what happens under the hood with minimal cognitive friction.

---

### **12. Clarity Is a Feature**

The highest compliment this DSL can receive is: *"I understood it without reading the docs."*
If that stops being true, the design has failed.

---

## **Related Topics**

- [**Getting Started**](getting-started.md) - Installation, first API call, basic setup
- [**Fluent Syntax Reference**](fluentsyntax.md) - Complete grammar and method reference
- [**Examples**](examples.md) - Real-world scenarios and complete examples
- [**Architecture Overview**](architectureanddesign.md) - Internal design and implementation
- [**Dependency Injection Guide**](di.md) - DI patterns and ServiceCollectionExtensions
- [**Configuration**](configuration.md) - Base URLs, timeouts, default headers, DI setup
- [**Request Building**](request-building.md) - Headers, query params, path params, cookies
- [**HTTP Verbs**](http-verbs.md) - GET, POST, PUT, PATCH, DELETE with examples
- [**Assertions**](assertions.md) - ShouldReturn variations, validation patterns
- [**Authentication**](authentication.md) - Auth providers, caching, per-user tokens
- [**Testing Guide**](testing-guide.md) - Unit testing with mocks, integration testing
- [**Extensibility**](extensibility.md) - Custom executors, validators, auth providers
- [**API Reference**](api-reference.md) - Complete interface and class documentation
- [**Contributing**](contributing.md) - Architecture internals and contribution guidelines
