using WireMock.Server;
using WireMock.Settings;

namespace NaturalApi.Integration.Tests.RestSharp.Common;

/// <summary>
/// WireMock server helper for RestSharp integration tests.
/// </summary>
public class WireMockServers
{
    private WireMockServer? _server;

    /// <summary>
    /// Gets the base URL of the WireMock server.
    /// </summary>
    public string BaseUrl => _server?.Urls?.FirstOrDefault() ?? "http://localhost:5000";

    /// <summary>
    /// Starts the WireMock server.
    /// </summary>
    public void Start()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Use any available port
            StartAdminInterface = true
        });
    }

    /// <summary>
    /// Stops the WireMock server.
    /// </summary>
    public void Stop()
    {
        _server?.Stop();
        _server?.Dispose();
    }

    /// <summary>
    /// Sets up a GET endpoint with the specified response.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="headers">Optional response headers</param>
    public void SetupGet(string path, int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody)
                .WithHeaders(headers ?? new Dictionary<string, string>()));
    }

    /// <summary>
    /// Sets up a POST endpoint with the specified response.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="headers">Optional response headers</param>
    public void SetupPost(string path, int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingPost())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody)
                .WithHeaders(headers ?? new Dictionary<string, string>()));
    }

    /// <summary>
    /// Sets up a PUT endpoint with the specified response.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="headers">Optional response headers</param>
    public void SetupPut(string path, int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingPut())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody)
                .WithHeaders(headers ?? new Dictionary<string, string>()));
    }

    /// <summary>
    /// Sets up a DELETE endpoint with the specified response.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="headers">Optional response headers</param>
    public void SetupDelete(string path, int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingDelete())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody)
                .WithHeaders(headers ?? new Dictionary<string, string>()));
    }

    /// <summary>
    /// Sets up a GET endpoint that ONLY responds if the Authorization header matches the expected value.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="expectedAuthHeader">Expected Authorization header value</param>
    public void SetupGetWithAuthValidation(string path, int statusCode, string responseBody, string expectedAuthHeader)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingGet()
            .WithHeader("Authorization", expectedAuthHeader))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody));
    }

    /// <summary>
    /// Sets up a GET endpoint that ONLY responds if NO Authorization header is present.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    public void SetupGetWithoutAuth(string path, int statusCode, string responseBody)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingGet()
            .WithHeader("Authorization", ".*", WireMock.Matchers.MatchBehaviour.RejectOnMatch))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody));
    }

    /// <summary>
    /// Sets up a GET endpoint that ONLY responds if ALL specified headers match.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="expectedHeaders">Expected headers</param>
    public void SetupGetWithHeadersValidation(string path, int statusCode, string responseBody, IDictionary<string, string> expectedHeaders)
    {
        var requestBuilder = WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingGet();

        foreach (var header in expectedHeaders)
        {
            requestBuilder = requestBuilder.WithHeader(header.Key, header.Value);
        }

        _server!.Given(requestBuilder)
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody));
    }

    /// <summary>
    /// Sets up a PATCH endpoint with the specified response.
    /// </summary>
    /// <param name="path">Endpoint path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseBody">Response body</param>
    /// <param name="headers">Optional response headers</param>
    public void SetupPatch(string path, int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _server!.Given(WireMock.RequestBuilders.Request.Create()
            .WithPath(path)
            .UsingPatch())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithBody(responseBody)
                .WithHeaders(headers ?? new Dictionary<string, string>()));
    }
}
