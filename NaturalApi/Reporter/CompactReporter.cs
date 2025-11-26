using System;
using NaturalApi.Reporter;

namespace NaturalApi.Reporter
{
    /// <summary>
    /// A compact reporter implementation that prints minimal request/response info.
    /// Intended as a small example for configuration-driven selection.
    /// </summary>
    public class CompactReporter : INaturalReporter
    {
        public void OnRequestSent(ApiRequestSpec request)
        {
            Console.WriteLine($"REQ {request.Method} {request.Endpoint}");
        }

        public void OnResponseReceived(IApiResultContext response)
        {
            Console.WriteLine($"RES {response.StatusCode} ({response.Duration}ms)");
        }

        public void OnAssertionPassed(string message, ApiResultContext response)
        {
            Console.WriteLine($"PASS: {message}");
        }

        public void OnAssertionFailed(string message, ApiResultContext response)
        {
            Console.WriteLine($"FAIL: {message}");
        }
    }
}
