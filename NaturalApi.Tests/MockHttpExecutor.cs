using NaturalApi;

namespace NaturalApi.Tests;

/// <summary>
/// Mock HTTP executor for testing purposes.
/// Provides a simple implementation that returns mock results.
/// </summary>
internal class MockHttpExecutor : IHttpExecutor
{
    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        // Create a mock response message
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"Mock response\"}")
        };

        // Create a mock result context
        return new MockApiResultContext(response);
    }
}

/// <summary>
/// Mock API result context for testing purposes.
/// Provides a simple implementation of the result context interface.
/// </summary>
internal class MockApiResultContext : IApiResultContext
{
    public HttpResponseMessage Response { get; }
    public int StatusCode { get; }
    public IDictionary<string, string> Headers { get; }
    public string RawBody { get; }

    public MockApiResultContext(HttpResponseMessage response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        StatusCode = (int)response.StatusCode;
        Headers = new Dictionary<string, string>();
        RawBody = "{\"message\":\"Mock response\"}";
    }

    public T BodyAs<T>()
    {
        // Simple mock implementation - in real tests this would deserialize properly
        if (typeof(T) == typeof(string))
        {
            return (T)(object)RawBody;
        }
        
        // For other types, return default value
        return default(T)!;
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
        return default(T)!;
    }

    public IApiResultContext Then(Action<IApiResult> next)
    {
        // Mock implementation - in real tests this would execute the action
        var result = new ApiResult(this, new MockHttpExecutor());
        next?.Invoke(result);
        return this;
    }
}
