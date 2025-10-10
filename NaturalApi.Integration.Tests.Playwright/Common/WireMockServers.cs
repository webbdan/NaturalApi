using WireMock.Server;
using WireMock.Settings;

namespace NaturalApi.Integration.Tests.Playwright.Common;

/// <summary>
/// WireMock server helper for Playwright integration tests.
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
