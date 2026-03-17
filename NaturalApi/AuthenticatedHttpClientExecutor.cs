using NaturalApi.Reporter;
using System.Net.Http.Headers;

namespace NaturalApi;

/// <summary>
/// HttpClient-based implementation of IAuthenticatedHttpExecutor.
/// Executes HTTP requests with authentication support.
/// Shares all request-building logic with HttpClientExecutor via HttpRequestHelper.
/// </summary>
public class AuthenticatedHttpClientExecutor : IAuthenticatedHttpExecutor
{
    private readonly HttpClient _httpClient;
    private INaturalReporter _reporter;

    public AuthenticatedHttpClientExecutor(HttpClient httpClient, INaturalReporter? reporter = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _reporter = reporter ?? new DefaultReporter();
    }

    public INaturalReporter Reporter { get => _reporter; set => _reporter = value; }

    /// <summary>
    /// Executes an HTTP request synchronously (backward compatibility).
    /// Prefer ExecuteAsync to avoid deadlocks.
    /// </summary>
    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        return ExecuteAsync(spec, null, null, null, true, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an HTTP request asynchronously (IHttpExecutor overload, no auth).
    /// </summary>
    public Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(spec, null, null, null, true, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with authentication resolution.
    /// </summary>
    public async Task<IApiResultContext> ExecuteAsync(
        ApiRequestSpec spec,
        IApiAuthProvider? authProvider,
        string? username,
        string? password,
        bool suppressAuth,
        CancellationToken cancellationToken = default)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        try
        {
            var url = HttpRequestHelper.BuildUrl(spec);
            var request = HttpRequestHelper.BuildRequest(spec, url);

            // Inject authentication — the only step that differs from HttpClientExecutor
            if (!suppressAuth && authProvider != null)
            {
                var token = await authProvider.GetAuthTokenAsync(username, password).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await HttpRequestHelper.SendAsync(_httpClient, request, spec, _reporter, this, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not ApiExecutionException)
        {
            throw new ApiExecutionException("Error during HTTP request execution", ex, spec);
        }
    }
}
