using NaturalApi;

namespace NaturalApi.Tests;

/// <summary>
/// Mock HTTP executor for testing purposes.
/// Provides a simple implementation that returns mock results.
/// </summary>
internal class MockHttpExecutor : IHttpExecutor
{
    public ApiRequestSpec LastSpec { get; private set; } = null!;
    
    private int _statusCode = 200;
    private string _responseBody = """{"message":"Mock response"}""";
    private IDictionary<string, string> _headers = new Dictionary<string, string>();

    public void SetupResponse(int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
        _headers = headers ?? new Dictionary<string, string>();
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        LastSpec = spec;
        
        // Create a mock response message
        var response = new HttpResponseMessage((System.Net.HttpStatusCode)_statusCode)
        {
            Content = new StringContent(_responseBody)
        };

        // Add headers to response
        foreach (var header in _headers)
        {
            response.Headers.Add(header.Key, header.Value);
        }

        // Create a mock result context
        return new MockApiResultContext(response, _responseBody, _headers, this);
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
    private readonly IHttpExecutor _httpExecutor;

    public MockApiResultContext(HttpResponseMessage response, string responseBody, IDictionary<string, string> headers, IHttpExecutor httpExecutor)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        StatusCode = (int)response.StatusCode;
        Headers = headers;
        RawBody = responseBody;
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
    }

    public T BodyAs<T>()
    {
        // Simple mock implementation - in real tests this would deserialize properly
        if (typeof(T) == typeof(string))
        {
            return (T)(object)RawBody;
        }
        
        // For JSON deserialization
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(RawBody) ?? default(T)!;
        }
        catch
        {
            return default(T)!;
        }
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
        // Check if T is ApiResponse<SomeType>
        if (typeof(T).IsGenericType && 
            typeof(T).GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var bodyType = typeof(T).GetGenericArguments()[0];
            
            // Use reflection to call BodyAs<T> with the body type
            var bodyAsMethod = typeof(MockApiResultContext).GetMethod("BodyAs")?.MakeGenericMethod(bodyType);
            var body = bodyAsMethod?.Invoke(this, null);
            
            // Create ApiResponse<T> instance
            var apiResponseType = typeof(ApiResponse<>).MakeGenericType(bodyType);
            return (T)Activator.CreateInstance(apiResponseType, this, _httpExecutor);
        }

        // Return just the deserialized body (existing behavior)
        return BodyAs<T>();
    }

    public IApiResultContext Then(Action<IApiResult> next)
    {
        // Mock implementation - in real tests this would execute the action
        var result = new ApiResponse<object>(this, _httpExecutor);
        next?.Invoke(result);
        return this;
    }

    public string? GetCookie(string name)
    {
        // Mock implementation - return null for testing
        return null;
    }
}
