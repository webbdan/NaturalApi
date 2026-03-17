using NaturalApi.Reporter;

namespace NaturalApi;

/// <summary>
/// HttpClient-based implementation of IHttpExecutor.
/// Executes HTTP requests using the provided HttpClient.
/// </summary>
public class HttpClientExecutor : IHttpExecutor
{
    private readonly HttpClient _httpClient;
    private INaturalReporter _reporter;

    /// <summary>
    /// Initializes a new instance of the HttpClientExecutor class.
    /// </summary>
    /// <param name="httpClient">HttpClient instance for making requests</param>
    /// <param name="reporter">Optional reporter for request/response logging</param>
    public HttpClientExecutor(HttpClient httpClient, INaturalReporter? reporter = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _reporter = reporter ?? new DefaultReporter();
    }

    public INaturalReporter Reporter { get => _reporter; set => _reporter = value; }

    /// <summary>
    /// Executes an HTTP request synchronously based on the provided specification.
    /// </summary>
    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        try
        {
            var url = HttpRequestHelper.BuildUrl(spec);
            var request = HttpRequestHelper.BuildRequest(spec, url);
            return HttpRequestHelper.SendSync(_httpClient, request, spec, _reporter, this);
        }
        catch (Exception ex) when (ex is not ApiExecutionException)
        {
            throw new ApiExecutionException("Error during HTTP request execution", ex, spec);
        }
    }

    /// <summary>
    /// Executes an HTTP request asynchronously based on the provided specification.
    /// This is the preferred method — avoids deadlocks in UI and ASP.NET contexts.
    /// </summary>
    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, CancellationToken cancellationToken = default)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        try
        {
            var url = HttpRequestHelper.BuildUrl(spec);
            var request = HttpRequestHelper.BuildRequest(spec, url);
            return await HttpRequestHelper.SendAsync(_httpClient, request, spec, _reporter, this, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not ApiExecutionException)
        {
            throw new ApiExecutionException("Error during HTTP request execution", ex, spec);
        }
    }
}
