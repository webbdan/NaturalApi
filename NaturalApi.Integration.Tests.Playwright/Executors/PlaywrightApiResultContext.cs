using Microsoft.Playwright;
using NaturalApi;
using System.Net.Http;

namespace NaturalApi.Integration.Tests.Playwright.Executors;

/// <summary>
/// Playwright-specific implementation of IApiResultContext.
/// Wraps Playwright's IAPIResponse for NaturalApi compatibility.
/// </summary>
public class PlaywrightApiResultContext : IApiResultContext
{
    private readonly IAPIResponse _response;
    private readonly PlaywrightHttpExecutor _executor;

    /// <summary>
    /// Initializes a new instance of the PlaywrightApiResultContext class.
    /// </summary>
    /// <param name="response">Playwright response</param>
    /// <param name="executor">Playwright executor for chaining</param>
    public PlaywrightApiResultContext(IAPIResponse response, PlaywrightHttpExecutor executor)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => _response.Status;

    /// <summary>
    /// Gets the response headers as a dictionary.
    /// </summary>
    public IDictionary<string, string> Headers => _response.Headers?.ToDictionary(h => h.Key, h => h.Value) ?? new Dictionary<string, string>();

    /// <summary>
    /// Gets the raw response body as a string.
    /// </summary>
    public string RawBody => _response.TextAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Gets the Playwright response.
    /// </summary>
    public IAPIResponse PlaywrightResponse => _response;

    /// <summary>
    /// Gets the HTTP response message (for compatibility with IApiResultContext).
    /// </summary>
    public HttpResponseMessage Response => CreateHttpResponseMessage();

    /// <summary>
    /// Deserializes the response body into the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns>Deserialized object</returns>
    public T BodyAs<T>()
    {
        var rawBody = RawBody;
        if (string.IsNullOrEmpty(rawBody))
        {
            throw new InvalidOperationException("Response body is empty or null");
        }

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<T>(rawBody, options) 
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

        // Return the deserialized body
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

        var result = new PlaywrightApiResult(this, _executor);
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
        var setCookieHeaders = Headers.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
        foreach (var setCookieHeader in setCookieHeaders)
        {
            var cookieValue = ExtractCookieValue(setCookieHeader.Value, name);
            if (cookieValue != null)
                return cookieValue;
        }

        return null;
    }

    /// <summary>
    /// Extracts a cookie value from a Set-Cookie header.
    /// </summary>
    /// <param name="setCookieHeader">The Set-Cookie header value</param>
    /// <param name="cookieName">The name of the cookie to extract</param>
    /// <returns>Cookie value if found, null otherwise</returns>
    private string? ExtractCookieValue(string? setCookieHeader, string cookieName)
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

    /// <summary>
    /// Creates an HttpResponseMessage from the Playwright response for compatibility.
    /// </summary>
    /// <returns>HttpResponseMessage representation</returns>
    private HttpResponseMessage CreateHttpResponseMessage()
    {
        var response = new HttpResponseMessage((System.Net.HttpStatusCode)StatusCode);
        
        // Add headers
        foreach (var header in Headers)
        {
            response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        // Add content
        if (!string.IsNullOrEmpty(RawBody))
        {
            response.Content = new StringContent(RawBody, System.Text.Encoding.UTF8, "application/json");
        }
        
        return response;
    }
}

/// <summary>
/// Playwright-specific implementation of IApiResult.
/// </summary>
public class PlaywrightApiResult : IApiResult
{
    private readonly PlaywrightApiResultContext _context;
    private readonly PlaywrightHttpExecutor _executor;

    public PlaywrightApiResult(PlaywrightApiResultContext context, PlaywrightHttpExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public dynamic Body => _context.RawBody;
    public int StatusCode => _context.StatusCode;
    public IDictionary<string, string> Headers => _context.Headers;
    public string RawBody => _context.RawBody;
    public HttpResponseMessage Response => _context.Response;

    public T BodyAs<T>()
    {
        return _context.BodyAs<T>();
    }

    public T ShouldReturn<T>()
    {
        return _context.ShouldReturn<T>();
    }

    public IApiContext For(string endpoint)
    {
        return new Api(_executor).For(endpoint);
    }

    public IApiResultContext ShouldBeSuccessful()
    {
        if (_context.StatusCode < 200 || _context.StatusCode >= 300)
        {
            throw new ApiAssertionException(
                $"Expected successful status code (2xx), but got {_context.StatusCode}",
                "Successful status code (2xx)",
                $"Status: {_context.StatusCode}",
                "Unknown endpoint",
                "Unknown method",
                _context.RawBody
            );
        }
        return _context;
    }

    public IApiResultContext ShouldHaveStatusCode(int statusCode)
    {
        if (_context.StatusCode != statusCode)
        {
            throw new ApiAssertionException(
                $"Expected status code {statusCode} but got {_context.StatusCode}",
                $"Status code {statusCode}",
                $"Status code {_context.StatusCode}",
                "Unknown endpoint",
                "Unknown method",
                _context.RawBody
            );
        }
        return _context;
    }

    public IApiResultContext And => _context;
}
