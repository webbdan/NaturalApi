// AIModified:2025-10-09T07:22:36Z
using Microsoft.Extensions.DependencyInjection;

namespace NaturalApi;

/// <summary>
/// Extension methods for registering NaturalApi services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers NaturalApi services with the default configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureDefaults">Optional action to configure default settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Action<NaturalApiOptions>? configureDefaults = null)
    {
        var options = new NaturalApiOptions();
        configureDefaults?.Invoke(options);

        // Register the API instance
        services.AddScoped<IApi>(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            var executor = new HttpClientExecutor(httpClient);
            var defaults = provider.GetService<IApiDefaultsProvider>();
            
            if (defaults != null)
            {
                return new Api(executor, defaults);
            }
            else
            {
                return new Api(executor);
            }
        });

        // Register default implementations if not already registered
        if (options.RegisterDefaults && !services.Any(s => s.ServiceType == typeof(IApiDefaultsProvider)))
        {
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }

        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom defaults provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="defaultsProvider">The defaults provider implementation</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TDefaults>(this IServiceCollection services, TDefaults defaultsProvider)
        where TDefaults : class, IApiDefaultsProvider
    {
        services.AddSingleton<IApiDefaultsProvider>(defaultsProvider);
        return services.AddNaturalApi(options => options.RegisterDefaults = false);
    }

    /// <summary>
    /// Registers NaturalApi services with a custom auth provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="authProvider">The auth provider implementation</param>
    /// <param name="configureDefaults">Optional action to configure default settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApiWithAuth<TAuth>(this IServiceCollection services, TAuth authProvider, Action<NaturalApiOptions>? configureDefaults = null)
        where TAuth : class, IApiAuthProvider
    {
        services.AddSingleton<IApiAuthProvider>(authProvider);
        return services.AddNaturalApi(configureDefaults);
    }

    /// <summary>
    /// Registers NaturalApi services with both custom defaults and auth providers.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="defaultsProvider">The defaults provider implementation</param>
    /// <param name="authProvider">The auth provider implementation</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApiWithAuth<TDefaults, TAuth>(this IServiceCollection services, TDefaults defaultsProvider, TAuth authProvider)
        where TDefaults : class, IApiDefaultsProvider
        where TAuth : class, IApiAuthProvider
    {
        services.AddSingleton<IApiDefaultsProvider>(defaultsProvider);
        services.AddSingleton<IApiAuthProvider>(authProvider);
        return services.AddNaturalApi(options => options.RegisterDefaults = false);
    }

    /// <summary>
    /// Registers NaturalApi services with a factory for creating the API instance.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="apiFactory">Factory function for creating the API instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Func<IServiceProvider, IApi> apiFactory)
    {
        services.AddScoped<IApi>(apiFactory);
        return services;
    }
}

/// <summary>
/// Configuration options for NaturalApi registration.
/// </summary>
public class NaturalApiOptions
{
    /// <summary>
    /// Whether to register default implementations.
    /// </summary>
    public bool RegisterDefaults { get; set; } = true;
}
