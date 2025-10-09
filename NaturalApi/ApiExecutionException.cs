using System.Net.Sockets;

namespace NaturalApi;

/// <summary>
/// Exception thrown when an API request fails during execution.
/// Wraps low-level HttpClient exceptions with meaningful context about the request that failed.
/// </summary>
public class ApiExecutionException : Exception
{
    /// <summary>
    /// The endpoint URL that was being called
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// The HTTP method that was used
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// The headers that were sent with the request
    /// </summary>
    public IDictionary<string, string> Headers { get; }

    /// <summary>
    /// The request body that was sent (if any)
    /// </summary>
    public object? Body { get; }

    /// <summary>
    /// The query parameters that were used
    /// </summary>
    public IDictionary<string, object> QueryParams { get; }

    /// <summary>
    /// The path parameters that were used
    /// </summary>
    public IDictionary<string, object> PathParams { get; }

    /// <summary>
    /// The timeout that was set for the request
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the ApiExecutionException class.
    /// </summary>
    /// <param name="message">Error message describing what went wrong</param>
    /// <param name="innerException">The underlying exception that caused the failure</param>
    /// <param name="spec">The request specification that failed</param>
    public ApiExecutionException(string message, Exception innerException, ApiRequestSpec spec)
        : base(message, innerException)
    {
        Endpoint = spec.Endpoint;
        Method = spec.Method;
        Headers = spec.Headers;
        Body = spec.Body;
        QueryParams = spec.QueryParams;
        PathParams = spec.PathParams;
        Timeout = spec.Timeout;
    }

    /// <summary>
    /// Creates a detailed string representation of the exception including request context.
    /// </summary>
    /// <returns>Formatted string with request details and error information</returns>
    public override string ToString()
    {
        var details = new List<string>
        {
            $"[{Method}] {Endpoint} failed: {Message}",
            $"Inner Exception: {InnerException?.GetType().Name} - {InnerException?.Message}"
        };

        if (Headers.Count > 0)
        {
            details.Add($"Headers: {string.Join(", ", Headers.Select(h => $"{h.Key}={h.Value}"))}");
        }

        if (QueryParams.Count > 0)
        {
            details.Add($"Query Params: {string.Join(", ", QueryParams.Select(q => $"{q.Key}={q.Value}"))}");
        }

        if (PathParams.Count > 0)
        {
            details.Add($"Path Params: {string.Join(", ", PathParams.Select(p => $"{p.Key}={p.Value}"))}");
        }

        if (Body != null)
        {
            details.Add($"Body: {Body.GetType().Name}");
        }

        if (Timeout.HasValue)
        {
            details.Add($"Timeout: {Timeout.Value.TotalSeconds}s");
        }

        return string.Join("\n", details);
    }

    /// <summary>
    /// Gets a user-friendly error message that includes the request context.
    /// </summary>
    /// <returns>Formatted error message</returns>
    public string GetUserFriendlyMessage()
    {
        var baseMessage = $"[{Method}] {Endpoint} failed: {Message}";
        
        if (InnerException != null)
        {
            var innerType = InnerException.GetType().Name;
            var innerMessage = InnerException.Message;
            
            // Provide more user-friendly messages for common exception types
            var friendlyMessage = innerType switch
            {
                nameof(HttpRequestException) => "Network connection failed",
                nameof(TaskCanceledException) => "Request timed out",
                nameof(SocketException) => "Connection to server failed",
                nameof(TimeoutException) => "Request exceeded timeout",
                _ => innerMessage
            };
            
            return $"{baseMessage}\nCause: {friendlyMessage}";
        }
        
        return baseMessage;
    }
}
