namespace NaturalApi;

/// <summary>
/// HTTP executor that supports authentication resolution.
/// Extends IHttpExecutor with authentication capabilities.
/// </summary>
public interface IAuthenticatedHttpExecutor : IHttpExecutor
{
    /// <summary>
    /// Executes an HTTP request with authentication resolution.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <param name="authProvider">Authentication provider for token resolution</param>
    /// <param name="username">Username context for per-user authentication</param>
    /// <param name="password">Password context for authentication</param>
    /// <param name="suppressAuth">Whether to suppress authentication for this request</param>
    /// <returns>Result context with response data and validation methods</returns>
    Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, string? password, bool suppressAuth);
}
