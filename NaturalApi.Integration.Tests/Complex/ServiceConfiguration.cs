using Microsoft.Extensions.DependencyInjection;
using NaturalApi;
using NaturalApi.Integration.Tests.Common;

namespace NaturalApi.Integration.Tests.Complex;

/// <summary>
/// Configuration for dependency injection in integration tests.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures services for integration testing with authentication.
    /// </summary>
    /// <param name="authServerUrl">The WireMock auth server URL</param>
    /// <param name="apiServerUrl">The WireMock API server URL</param>
    /// <param name="username">Default username for authentication</param>
    /// <param name="password">Default password for authentication</param>
    /// <param name="cacheExpiration">Token cache expiration time</param>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider ConfigureServices(
        string authServerUrl, 
        string apiServerUrl, 
        string username, 
        string password,
        TimeSpan? cacheExpiration = null)
    {
        var services = new ServiceCollection();

        // Configure HttpClient for auth service
        services.AddHttpClient<IUsernamePasswordAuthService, UsernamePasswordAuthService>(client =>
        {
            client.BaseAddress = new Uri(authServerUrl);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

        // Configure HttpClient for NaturalApi (pointing to API server)
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(apiServerUrl);
        });

        // Use the clean API with custom API factory for authentication
        services.AddNaturalApi<CustomAuthProvider, CustomApi>("ApiClient", 
            provider =>
            {
                var authService = provider.GetRequiredService<IUsernamePasswordAuthService>();
                return new CustomAuthProvider(authService, username, password);
            },
            provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var executor = new HttpClientExecutor(httpClient);
                var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
                return new CustomApi(executor, defaults, httpClient);
            });


        return services.BuildServiceProvider();
    }
}
