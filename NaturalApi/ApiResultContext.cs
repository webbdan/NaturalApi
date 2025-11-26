namespace NaturalApi;

/// <summary>
/// Real implementation of IApiResultContext that wraps HTTP responses.
/// Provides access to response data and validation methods.
/// </summary>
public class ApiResultContext : IApiResultContext
{
    /// <summary>
    /// Gets the raw HTTP response message.
    /// </summary>
    public HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Duration of the request in ms
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// Gets the response headers as a dictionary.
    /// </summary>
    public IDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the raw response body as a string.
    /// </summary>
    public string RawBody { get; }

    private readonly IHttpExecutor _httpExecutor;

    /// <summary>
    /// Initializes a new instance of the ApiResultContext class.
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <param name="httpExecutor">HTTP executor for chaining</param>
    public ApiResultContext(HttpResponseMessage response, long duration, IHttpExecutor httpExecutor)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        _httpExecutor = httpExecutor ?? throw new ArgumentNullException(nameof(httpExecutor));
        StatusCode = (int)response.StatusCode;
        Duration = duration;
        // Combine response headers and content headers
        var allHeaders = new Dictionary<string, string>();
        
        // Add response headers
        foreach (var header in response.Headers)
        {
            allHeaders[header.Key] = string.Join(", ", header.Value);
        }
        
        // Add content headers
        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                allHeaders[header.Key] = string.Join(", ", header.Value);
            }
        }
        
        Headers = allHeaders;
        RawBody = ReadResponseBody(response);
    }

    /// <summary>
    /// Reads the response body as a string.
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <returns>Response body as string</returns>
    private string ReadResponseBody(HttpResponseMessage response)
    {
        try
        {
            return response.Content.ReadAsStringAsync().Result;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Deserializes the response body into the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns>Deserialized object</returns>
    public T BodyAs<T>()
    {
        if (string.IsNullOrEmpty(RawBody))
        {
            throw new InvalidOperationException("Response body is empty or null");
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(RawBody) 
                ?? throw new InvalidOperationException("Deserialization returned null");
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates the response using fluent assertions.
    /// </summary>
    /// <typeparam name="T">Expected response body type</typeparam>
    /// <param name="status">Expected status code (optional)</param>
    /// <param name="bodyValidator">Body validation function (optional)</param>
    /// <param name="headers">Header validation function (optional)</param>
    /// <returns>This result context for chaining</returns>
    public IApiResultContext ShouldReturn<T>(
        int? status = null,
        Func<T, bool>? bodyValidator = null,
        Func<IDictionary<string, string>, bool>? headers = null)
    {
        // Validate status code if specified
        if (status.HasValue && StatusCode != status.Value)
        {
            throw new ApiAssertionException(
                $"Expected status code {status.Value} but got {StatusCode}",
                $"Status code {status.Value}",
                $"Status code {StatusCode}",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }

        // Validate headers if specified
        if (headers != null && !headers(Headers))
        {
            throw new ApiAssertionException(
                "Header validation failed",
                "Valid headers",
                "Invalid headers",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }

        // Validate body if specified
        if (bodyValidator != null)
        {
            try
            {
                var body = BodyAs<T>();
                if (!bodyValidator(body))
                {
                    throw new ApiAssertionException(
                        "Body validation failed",
                        "Valid body",
                        "Invalid body",
                        "Unknown endpoint",
                        "Unknown method",
                        RawBody
                    );
                }
            }
            catch (Exception ex) when (!(ex is ApiAssertionException))
            {
                throw new ApiAssertionException(
                    $"Body validation failed: {ex.Message}",
                    "Valid body",
                    "Invalid body",
                    "Unknown endpoint",
                    "Unknown method",
                    RawBody
                );
            }
        }

        return this;
    }

    /// <summary>
    /// Validates the response status code only.
    /// </summary>
    /// <param name="status">Expected status code</param>
    /// <returns>This result context for chaining</returns>
    public IApiResultContext ShouldReturn(int status)
    {
        if (StatusCode != status)
        {
            throw new ApiAssertionException(
                $"Expected status code {status} but got {StatusCode}",
                $"Status code {status}",
                $"Status code {StatusCode}",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }
        return this;
    }

    /// <summary>
    /// Validates the response body only.
    /// </summary>
    /// <typeparam name="T">Expected response body type</typeparam>
    /// <param name="bodyValidator">Body validation function</param>
    /// <returns>This result context for chaining</returns>
    public IApiResultContext ShouldReturn<T>(Func<T, bool> bodyValidator)
    {
        try
        {
            var body = BodyAs<T>();
            if (!bodyValidator(body))
            {
                throw new ApiAssertionException(
                    "Body validation failed",
                    "Valid body",
                    "Invalid body",
                    "Unknown endpoint",
                    "Unknown method",
                    RawBody
                );
            }
        }
        catch (Exception ex) when (!(ex is ApiAssertionException))
        {
            throw new ApiAssertionException(
                $"Body validation failed: {ex.Message}",
                "Valid body",
                "Invalid body",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }
        return this;
    }

    /// <summary>
    /// Validates the response status and headers.
    /// </summary>
    /// <param name="status">Expected status code</param>
    /// <param name="headers">Header validation function</param>
    /// <returns>This result context for chaining</returns>
    public IApiResultContext ShouldReturn(int status, Func<IDictionary<string, string>, bool> headers)
    {
        if (StatusCode != status)
        {
            throw new ApiAssertionException(
                $"Expected status code {status} but got {StatusCode}",
                $"Status code {status}",
                $"Status code {StatusCode}",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }

        if (!headers(Headers))
        {
            throw new ApiAssertionException(
                "Header validation failed",
                "Valid headers",
                "Invalid headers",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }

        return this;
    }

    /// <summary>
    /// Validates the response and returns the deserialized object.
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <returns>The deserialized response object</returns>
    public T ShouldReturn<T>()
    {
        // Validate status code is successful (2xx)
        if (StatusCode < 200 || StatusCode >= 300)
        {
            throw new ApiAssertionException(
                $"Expected successful status code (2xx), but got {StatusCode}",
                "Successful status code (2xx)",
                $"Status: {StatusCode}",
                "Unknown endpoint",
                "Unknown method",
                RawBody
            );
        }

        // Check if T is ApiResponse<SomeType>
        if (typeof(T).IsGenericType && 
            typeof(T).GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var bodyType = typeof(T).GetGenericArguments()[0];
            
            // Use reflection to call BodyAs<T> with the body type
            var bodyAsMethod = typeof(ApiResultContext).GetMethod("BodyAs")?.MakeGenericMethod(bodyType);
            var body = bodyAsMethod?.Invoke(this, null);
            
            // Create ApiResponse<T> instance
            var apiResponseType = typeof(ApiResponse<>).MakeGenericType(bodyType);
            return (T)Activator.CreateInstance(apiResponseType, this, _httpExecutor);
        }

        // Return just the deserialized body (existing behavior)
        return BodyAs<T>();
    }

    /// <summary>
    /// Allows chaining additional operations or validations.
    /// </summary>
    /// <param name="next">Action to perform on this result</param>
    /// <returns>This result context for chaining</returns>
    public IApiResultContext Then(Action<IApiResult> next)
    {
        if (next == null)
            throw new ArgumentNullException(nameof(next));

        var result = new ApiResult(this, _httpExecutor);
        next(result);
        return this;
    }

    /// <summary>
    /// Gets a cookie value from the response headers.
    /// </summary>
    /// <param name="name">Cookie name</param>
    /// <returns>Cookie value if found, null otherwise</returns>
    public string? GetCookie(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Look for Set-Cookie headers
        if (Response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var setCookieHeader in setCookieHeaders)
            {
                var cookieValue = ExtractCookieValue(setCookieHeader, name);
                if (cookieValue != null)
                    return cookieValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts a cookie value from a Set-Cookie header.
    /// </summary>
    /// <param name="setCookieHeader">The Set-Cookie header value</param>
    /// <param name="cookieName">The name of the cookie to extract</param>
    /// <returns>Cookie value if found, null otherwise</returns>
    private string? ExtractCookieValue(string setCookieHeader, string cookieName)
    {
        if (string.IsNullOrWhiteSpace(setCookieHeader) || string.IsNullOrWhiteSpace(cookieName))
            return null;

        // Split by semicolon to get cookie parts
        var parts = setCookieHeader.Split(';');
        if (parts.Length == 0)
            return null;

        // First part should be the cookie name=value
        var cookiePart = parts[0].Trim();
        if (cookiePart.StartsWith($"{cookieName}=", StringComparison.OrdinalIgnoreCase))
        {
            return cookiePart.Substring(cookieName.Length + 1);
        }

        return null;
    }
}
