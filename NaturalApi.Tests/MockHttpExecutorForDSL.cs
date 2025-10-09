using System.Net;
using System.Net.Http;
using System.Text;

namespace NaturalApi.Tests;

/// <summary>
/// Mock HTTP executor that returns canned responses for DSL testing.
/// </summary>
public class MockHttpExecutorForDSL : IHttpExecutor
{
    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        return ExecuteAsync(spec).Result;
    }

    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec)
    {
        // Create a mock response based on the endpoint
        var response = new HttpResponseMessage();
        
        if (spec.Endpoint.Contains("/users/") && spec.Method == HttpMethod.Post)
        {
            // POST /users/{id} - Create user
            response.StatusCode = HttpStatusCode.Created;
            response.Content = new StringContent(
                """{"Id":123,"Name":"Dan","Email":"dan@test.local","Roles":["admin","tester"]}""",
                Encoding.UTF8,
                "application/json");
        }
        else if (spec.Endpoint.Contains("/users/123") && spec.Method == HttpMethod.Get)
        {
            // GET /users/123 - Get user
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(
                """{"Id":123,"Name":"Dan","Email":"dan@test.local","Roles":["admin","tester"]}""",
                Encoding.UTF8,
                "application/json");
        }
        else
        {
            // Default response
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        // Add headers
        response.Headers.Add("X-Request-ID", "test-123");

        return new MockApiResultContextForDSL(response);
    }
}

/// <summary>
/// Mock API result context for DSL testing.
/// </summary>
public class MockApiResultContextForDSL : IApiResultContext
{
    public HttpResponseMessage Response { get; }
    public int StatusCode { get; }
    public IDictionary<string, string> Headers { get; }
    public string RawBody { get; }

    public MockApiResultContextForDSL(HttpResponseMessage response)
    {
        Response = response;
        StatusCode = (int)response.StatusCode;
        Headers = new Dictionary<string, string>();
        
        // Add response headers
        foreach (var header in response.Headers)
        {
            Headers[header.Key] = string.Join(", ", header.Value);
        }
        
        // Add content headers
        foreach (var header in response.Content.Headers)
        {
            Headers[header.Key] = string.Join(", ", header.Value);
        }

        RawBody = response.Content.ReadAsStringAsync().Result;
    }

    public T BodyAs<T>()
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(RawBody)!;
    }

    public IApiResultContext ShouldReturn<T>(
        int? status = null,
        Func<T, bool>? bodyValidator = null,
        Func<IDictionary<string, string>, bool>? headers = null)
    {
        // Mock implementation - in real tests this would perform actual validation
        return this;
    }

    public IApiResultContext ShouldReturn(int status)
    {
        // Mock implementation - in real tests this would perform actual validation
        return this;
    }

    public IApiResultContext ShouldReturn<T>(Func<T, bool> bodyValidator)
    {
        // Mock implementation - in real tests this would perform actual validation
        return this;
    }

    public IApiResultContext ShouldReturn(int status, Func<IDictionary<string, string>, bool> headers)
    {
        // Mock implementation - in real tests this would perform actual validation
        return this;
    }

    public T ShouldReturn<T>()
    {
        // Mock implementation - in real tests this would validate and return deserialized object
        return BodyAs<T>();
    }

    public IApiResultContext Then(Action<IApiResult> next)
    {
        // Mock implementation - in real tests this would execute the action
        var result = new ApiResult(this, new MockHttpExecutorForDSL());
        next?.Invoke(result);
        return this;
    }
}
