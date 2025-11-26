using NaturalApi.Reporter;
using System.Net;
using System.Net.Http;
using System.Text;

namespace NaturalApi.Tests;

/// <summary>
/// Mock HTTP executor that simulates timeout behavior for testing.
/// </summary>
public class MockTimeoutHttpExecutor : IHttpExecutor
{
    private INaturalReporter _reporter = new NullReporter();
    public INaturalReporter Reporter { get => _reporter; set => _reporter = value ?? new NullReporter(); }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        return ExecuteAsync(spec).Result;
    }

    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec)
    {
        // Use per-request reporter if provided, otherwise executor reporter
        var reporterToUse = spec.Reporter ?? _reporter;
        reporterToUse.OnRequestSent(spec);

        // Simulate timeout behavior based on the endpoint
        if (spec.Endpoint.Contains("delay"))
        {
            // Extract delay from endpoint (e.g., "delay/2" = 2 seconds)
            var delayMatch = System.Text.RegularExpressions.Regex.Match(spec.Endpoint, @"delay/(\d+)");
            if (delayMatch.Success && int.TryParse(delayMatch.Groups[1].Value, out int delaySeconds))
            {
                // Check if the request timeout is shorter than the delay
                if (spec.Timeout.HasValue && spec.Timeout.Value.TotalSeconds < delaySeconds)
                {
                    // Simulate timeout by throwing TaskCanceledException
                    throw new TaskCanceledException("The operation was canceled due to timeout.");
                }

                // If no timeout or timeout is longer than delay, simulate successful response
                await Task.Delay(100); // Small delay to simulate processing
            }
        }

        // Create a mock successful response
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"message\":\"success\"}", Encoding.UTF8, "application/json")
        };

        // Add some headers
        response.Headers.Add("X-Request-ID", "test-123");

        var result = new ApiResultContext(response, 0, this);

        reporterToUse.OnResponseReceived(result);

        return result;
    }
}
