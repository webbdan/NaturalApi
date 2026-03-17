using System.Diagnostics;

namespace NaturalApi;

/// <summary>
/// Shared helper for building and executing HTTP requests.
/// Eliminates duplication between HttpClientExecutor and AuthenticatedHttpClientExecutor.
/// </summary>
internal static class HttpRequestHelper
{
    /// <summary>
    /// Builds the full URL with path and query parameters.
    /// </summary>
    internal static string BuildUrl(ApiRequestSpec spec)
    {
        var url = spec.Endpoint;

        foreach (var param in spec.PathParams)
        {
            url = url.Replace($"{{{param.Key}}}", param.Value.ToString());
        }

        if (spec.QueryParams.Count > 0)
        {
            var queryString = string.Join("&",
                spec.QueryParams.Select(kvp =>
                    $"{kvp.Key}={Uri.EscapeDataString(kvp.Value.ToString() ?? "")}"));
            url += (url.Contains("?") ? "&" : "?") + queryString;
        }

        return url;
    }

    /// <summary>
    /// Creates an HttpRequestMessage from the spec, applying headers, cookies, and body.
    /// Does NOT apply authentication — that is the caller's responsibility.
    /// </summary>
    internal static HttpRequestMessage BuildRequest(ApiRequestSpec spec, string url)
    {
        var request = new HttpRequestMessage(spec.Method, url);

        // Add headers (excluding Content-Type which goes on content)
        foreach (var header in spec.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;
            request.Headers.Add(header.Key, header.Value);
        }

        // Add cookies
        if (spec.Cookies.Count > 0)
        {
            var cookieValues = spec.Cookies.Select(c => $"{c.Key}={c.Value}");
            request.Headers.Add("Cookie", string.Join("; ", cookieValues));
        }

        // Add body for POST, PUT, PATCH
        if (spec.Body != null &&
            (spec.Method == HttpMethod.Post || spec.Method == HttpMethod.Put || spec.Method == HttpMethod.Patch))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(spec.Body);
            var contentType = spec.Headers.ContainsKey("Content-Type")
                ? spec.Headers["Content-Type"].Split(';')[0].Trim()
                : "application/json";
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, contentType);
        }

        return request;
    }

    /// <summary>
    /// Sends the request synchronously with timeout, stopwatch, and reporter hooks.
    /// Returns the ApiResultContext.
    /// </summary>
    internal static ApiResultContext SendSync(
        HttpClient httpClient,
        HttpRequestMessage request,
        ApiRequestSpec spec,
        Reporter.INaturalReporter reporter,
        IHttpExecutor executor)
    {
        var reporterToUse = spec.Reporter ?? reporter;
        reporterToUse.OnRequestSent(spec);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;

        if (spec.Timeout.HasValue)
        {
            using var cts = new CancellationTokenSource(spec.Timeout.Value);
            response = httpClient.Send(request, cts.Token);
        }
        else
        {
            response = httpClient.Send(request);
        }

        sw.Stop();

        var result = new ApiResultContext(response, sw.ElapsedMilliseconds, executor);
        reporterToUse.OnResponseReceived(result);
        return result;
    }

    /// <summary>
    /// Sends the request asynchronously with timeout+cancellation, stopwatch, and reporter hooks.
    /// Returns the ApiResultContext.
    /// </summary>
    internal static async Task<ApiResultContext> SendAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        ApiRequestSpec spec,
        Reporter.INaturalReporter reporter,
        IHttpExecutor executor,
        CancellationToken cancellationToken)
    {
        var reporterToUse = spec.Reporter ?? reporter;
        reporterToUse.OnRequestSent(spec);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;

        if (spec.Timeout.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(spec.Timeout.Value);
            response = await httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
        }
        else
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        sw.Stop();

        var result = new ApiResultContext(response, sw.ElapsedMilliseconds, executor);
        reporterToUse.OnResponseReceived(result);
        return result;
    }
}
