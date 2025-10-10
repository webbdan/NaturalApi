using WireMock.Server;
using WireMock.Settings;

namespace NaturalApi.Integration.Tests.Common;

/// <summary>
/// Manages WireMock servers for authentication and API endpoints.
/// </summary>
public class WireMockServers : IDisposable
{
    public WireMockServer AuthServer { get; }
    public WireMockServer ApiServer { get; }
    
    public string AuthBaseUrl => $"http://localhost:{AuthServer.Ports[0]}";
    public string ApiBaseUrl => $"http://localhost:{ApiServer.Ports[0]}";

    public WireMockServers()
    {
        // Create auth server on random port
        AuthServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Random port
            StartAdminInterface = false
        });

        // Create API server on random port  
        ApiServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Random port
            StartAdminInterface = false
        });

        SetupAuthEndpoints();
        SetupApiEndpoints();
    }

    private void SetupAuthEndpoints()
    {
        // Valid user credentials
        AuthServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/auth/login")
                .UsingPost()
                .WithBody("{\"username\":\"testuser\",\"password\":\"testpass\"}"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"token\":\"valid-token-12345\",\"expiresIn\":600}"));

        // Invalid credentials
        AuthServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/auth/login")
                .UsingPost()
                .WithBody("{\"username\":\"invalid\",\"password\":\"invalid\"}"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"Invalid credentials\"}"));

        // Another valid user
        AuthServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/auth/login")
                .UsingPost()
                .WithBody("{\"username\":\"user2\",\"password\":\"pass2\"}"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"token\":\"valid-token-user2-67890\",\"expiresIn\":600}"));
    }

    private void SetupApiEndpoints()
    {
        // Protected endpoint that validates bearer token
        ApiServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/protected")
                .UsingGet()
                .WithHeader("Authorization", "Bearer valid-token-12345"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"message\":\"Access granted\",\"data\":\"protected-resource-data\"}"));

        // Protected endpoint for user2
        ApiServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/protected")
                .UsingGet()
                .WithHeader("Authorization", "Bearer valid-token-user2-67890"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"message\":\"Access granted for user2\",\"data\":\"user2-protected-data\"}"));

        // Invalid or missing token
        ApiServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/protected")
                .UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"Unauthorized\"}"));

        // Invalid token
        ApiServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/protected")
                .UsingGet()
                .WithHeader("Authorization", "Bearer invalid-token"))
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"Invalid token\"}"));
    }

    public void Dispose()
    {
        AuthServer?.Dispose();
        ApiServer?.Dispose();
    }
}
