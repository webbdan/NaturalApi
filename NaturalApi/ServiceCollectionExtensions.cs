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
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services)
    {
        // Register default HttpClient
        services.AddHttpClient();
        
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
        if (!services.Any(s => s.ServiceType == typeof(IApiDefaultsProvider)))
        {
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }

        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom auth provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="authProvider">The auth provider implementation</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        // Register default HttpClient
        services.AddHttpClient();
        
        services.AddSingleton<IApiAuthProvider>(authProvider);
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });
        
        // Register the API instance with automatic authentication
        services.AddScoped<IApi>(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
            return new Api(defaults, httpClient);
        });
        
        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HttpClient.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, HttpClient httpClient)
    {
        services.AddScoped<IApi>(provider =>
        {
            var executor = new HttpClientExecutor(httpClient);
            var defaults = provider.GetService<IApiDefaultsProvider>();
            return defaults != null ? new Api(executor, defaults) : new Api(executor);
        });
        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with both a custom HttpClient and auth provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="authProvider">The auth provider implementation</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, HttpClient httpClient, TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        services.AddSingleton<IApiAuthProvider>(authProvider);
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });
        return services.AddNaturalApi(httpClient);
    }

    /// <summary>
    /// Registers NaturalApi services with a named HttpClient.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, string httpClientName)
    {
        services.AddScoped<IApi>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);
            var executor = new HttpClientExecutor(httpClient);
            var defaults = provider.GetService<IApiDefaultsProvider>();
            return defaults != null ? new Api(executor, defaults) : new Api(executor);
        });
        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">The NaturalApi configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, NaturalApiConfiguration config)
    {
        // Register HttpClient if base URL is provided
        if (!string.IsNullOrEmpty(config.BaseUrl))
        {
            services.AddHttpClient("NaturalApiClient", client =>
            {
                client.BaseAddress = new Uri(config.BaseUrl);
            });
        }
        else if (!string.IsNullOrEmpty(config.HttpClientName))
        {
            // HttpClient is already registered by the user
        }
        else
        {
            // Register default HttpClient
            services.AddHttpClient();
        }

        // Register auth provider if provided
        if (config.AuthProvider != null)
        {
            services.AddSingleton<IApiAuthProvider>(config.AuthProvider);
            services.AddSingleton<IApiDefaultsProvider>(provider => 
            {
                var auth = provider.GetRequiredService<IApiAuthProvider>();
                return new DefaultApiDefaults(authProvider: auth);
            });
        }
        else
        {
            // Register default defaults provider
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }

        // Register the API instance
        services.AddScoped<IApi>(provider =>
        {
            HttpClient httpClient;
            
            if (!string.IsNullOrEmpty(config.BaseUrl))
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                httpClient = httpClientFactory.CreateClient("NaturalApiClient");
            }
            else if (!string.IsNullOrEmpty(config.HttpClientName))
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                httpClient = httpClientFactory.CreateClient(config.HttpClientName);
            }
            else
            {
                httpClient = provider.GetRequiredService<HttpClient>();
            }

            var defaults = provider.GetService<IApiDefaultsProvider>();
            return new Api(defaults, httpClient);
        });

        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with both a named HttpClient and auth provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <param name="authProvider">The auth provider implementation</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, string httpClientName, TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        services.AddSingleton<IApiAuthProvider>(authProvider);
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });
        
        // Register the API instance with automatic authentication
        services.AddScoped<IApi>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);
            var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
            return new Api(defaults, httpClient);
        });
        
        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with both a named HttpClient and auth provider factory.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <param name="authProviderFactory">Factory function for creating the auth provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, string httpClientName, Func<IServiceProvider, TAuth> authProviderFactory)
        where TAuth : class, IApiAuthProvider
    {
        services.AddSingleton<IApiAuthProvider>(authProviderFactory);
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });
        
        // Register the API instance with automatic authentication
        services.AddScoped<IApi>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);
            var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
            return new Api(defaults, httpClient);
        });
        
        return services;
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

    /// <summary>
    /// Registers NaturalApi services with a base URL and auth provider.
    /// The auth provider is responsible for its own URL configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="apiBaseUrl">Base URL for the API</param>
    /// <param name="authProvider">The auth provider implementation</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApiWithBaseUrl<TAuth>(
        this IServiceCollection services, 
        string apiBaseUrl, 
        TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        // Register HttpClient for API
        services.AddHttpClient("NaturalApiClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        // Register auth provider
        services.AddSingleton<IApiAuthProvider>(authProvider);
        
        // Register defaults with auth
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });
        
        // Register the API instance with automatic authentication
        services.AddScoped<IApi>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("NaturalApiClient");
            var defaults = provider.GetRequiredService<IApiDefaultsProvider>();
            return new Api(defaults, httpClient);
        });
        
        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom API factory and auth provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <param name="authProviderFactory">Factory function for creating the auth provider</param>
    /// <param name="apiFactory">Factory function for creating the API instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TAuth, TApi>(
        this IServiceCollection services, 
        string httpClientName, 
        Func<IServiceProvider, TAuth> authProviderFactory,
        Func<IServiceProvider, TApi> apiFactory)
        where TAuth : class, IApiAuthProvider
        where TApi : class, IApi
    {
        services.AddSingleton<IApiAuthProvider>(authProviderFactory);
        services.AddSingleton<IApiDefaultsProvider>(provider => 
        {
            var auth = provider.GetRequiredService<IApiAuthProvider>();
            return new DefaultApiDefaults(authProvider: auth);
        });
        services.AddScoped<IApi>(apiFactory);
        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HTTP executor.
    /// This enables using alternative HTTP clients like RestSharp, Playwright, etc.
    /// </summary>
    /// <typeparam name="TExecutor">The HTTP executor implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TExecutor>(this IServiceCollection services)
        where TExecutor : class, IHttpExecutor
    {
        // Register the custom executor
        services.AddScoped<IHttpExecutor, TExecutor>();
        
        // Register the API instance
        services.AddScoped<IApi>(provider =>
        {
            var executor = provider.GetRequiredService<IHttpExecutor>();
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
        if (!services.Any(s => s.ServiceType == typeof(IApiDefaultsProvider)))
        {
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }

        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HTTP executor using a factory.
    /// This enables using alternative HTTP clients like RestSharp, Playwright, etc.
    /// </summary>
    /// <typeparam name="TExecutor">The HTTP executor implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="executorFactory">Factory function for creating the executor</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TExecutor>(
        this IServiceCollection services, 
        Func<IServiceProvider, TExecutor> executorFactory)
        where TExecutor : class, IHttpExecutor
    {
        // Register the custom executor
        services.AddScoped<IHttpExecutor>(executorFactory);
        
        // Register the API instance
        services.AddScoped<IApi>(provider =>
        {
            var executor = provider.GetRequiredService<IHttpExecutor>();
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
        if (!services.Any(s => s.ServiceType == typeof(IApiDefaultsProvider)))
        {
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }

        return services;
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HTTP executor and options.
    /// This enables using alternative HTTP clients like RestSharp, Playwright, etc.
    /// </summary>
    /// <typeparam name="TExecutor">The HTTP executor implementation</typeparam>
    /// <typeparam name="TOptions">The options type for the executor</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure the executor options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi<TExecutor, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions)
        where TExecutor : class, IHttpExecutor
        where TOptions : class, new()
    {
        // Configure options
        var options = new TOptions();
        configureOptions(options);
        
        // Register options
        services.AddSingleton(options);
        
        // Register the custom executor with options
        services.AddScoped<IHttpExecutor>(provider =>
        {
            var optionsInstance = provider.GetRequiredService<TOptions>();
            return Activator.CreateInstance(typeof(TExecutor), optionsInstance) as TExecutor
                ?? throw new InvalidOperationException($"Failed to create {typeof(TExecutor).Name}");
        });
        
        // Register the API instance
        services.AddScoped<IApi>(provider =>
        {
            var executor = provider.GetRequiredService<IHttpExecutor>();
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
        if (!services.Any(s => s.ServiceType == typeof(IApiDefaultsProvider)))
        {
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }

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