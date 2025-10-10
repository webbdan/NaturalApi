using Microsoft.Extensions.DependencyInjection;
using NaturalApi;
using NaturalApi.Integration.Tests.Playwright.Executors;

namespace NaturalApi.Integration.Tests.Playwright.Common;

/// <summary>
/// Test helpers for Playwright integration tests.
/// </summary>
public static class PlaywrightTestHelpers
{
    /// <summary>
    /// Creates a service collection with Playwright executor configured.
    /// </summary>
    /// <param name="baseUrl">Base URL for the Playwright client</param>
    /// <returns>Configured service collection</returns>
    public static ServiceCollection CreateServiceCollection(string baseUrl)
    {
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor(baseUrl));
        services.AddSingleton<IApiDefaultsProvider>(new TestApiDefaults(null, baseUrl));
        return services;
    }

    /// <summary>
    /// Creates a service collection with Playwright executor and auth provider.
    /// </summary>
    /// <param name="baseUrl">Base URL for the Playwright client</param>
    /// <param name="authProvider">Auth provider to use</param>
    /// <returns>Configured service collection</returns>
    public static ServiceCollection CreateServiceCollectionWithAuth(string baseUrl, IApiAuthProvider authProvider)
    {
        var services = new ServiceCollection();
        services.AddNaturalApi<PlaywrightHttpExecutor>(provider => 
            new PlaywrightHttpExecutor(baseUrl));
        services.AddSingleton<IApiAuthProvider>(authProvider);
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new TestApiDefaults(auth, baseUrl);
        });
        return services;
    }

    /// <summary>
    /// Creates a Playwright executor directly for testing.
    /// </summary>
    /// <param name="baseUrl">Base URL for the Playwright client</param>
    /// <returns>Playwright executor instance</returns>
    public static PlaywrightHttpExecutor CreateExecutor(string baseUrl)
    {
        return new PlaywrightHttpExecutor(baseUrl);
    }

    /// <summary>
    /// Creates an API instance with Playwright executor for testing.
    /// </summary>
    /// <param name="baseUrl">Base URL for the Playwright client</param>
    /// <returns>API instance</returns>
    public static IApi CreateApi(string baseUrl)
    {
        var executor = CreateExecutor(baseUrl);
        return new Api(executor);
    }
}

/// <summary>
/// Test API defaults provider for Playwright tests.
/// </summary>
public class TestApiDefaults : IApiDefaultsProvider
{
    private readonly string? _baseUrl;

    public TestApiDefaults(IApiAuthProvider? authProvider = null, string? baseUrl = null)
    {
        AuthProvider = authProvider;
        _baseUrl = baseUrl;
    }

    public Uri? BaseUri => _baseUrl != null ? new Uri(_baseUrl) : new Uri("https://api.example.com");
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider { get; }
}
