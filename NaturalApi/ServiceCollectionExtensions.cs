using Microsoft.Extensions.DependencyInjection;
using NaturalApi.Reporter;
using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;

namespace NaturalApi;

/// <summary>
/// Extension methods for registering NaturalApi services with dependency injection.
/// 
/// The primary entry point is AddNaturalApi(Action&lt;NaturalApiBuilder&gt;), which supports
/// all configuration scenarios through a single builder API. Legacy overloads are kept
/// for backward compatibility but delegate to the builder internally.
/// </summary>
public static class ServiceCollectionExtensions
{
    // ──────────────────────────────────────────────────────────────
    //  PRIMARY ENTRY POINT — builder pattern
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers NaturalApi services using the builder pattern.
    /// This is the recommended way to configure NaturalApi.
    /// <example>
    /// <code>
    /// services.AddNaturalApi(api =>
    /// {
    ///     api.BaseUrl = "https://api.example.com";
    ///     api.AuthProvider = new MyAuthProvider();
    ///     api.ReporterName = "compact";
    /// });
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the builder</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Action<NaturalApiBuilder> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var builder = new NaturalApiBuilder();
        configure(builder);

        return services.ApplyNaturalApiBuilder(builder);
    }

    // ──────────────────────────────────────────────────────────────
    //  REPORTING — unchanged
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers reporter factory and common reporters using the default internal mapping.
    /// </summary>
    public static IServiceCollection AddNaturalApiReporting(this IServiceCollection services)
    {
        var defaultMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = typeof(DefaultReporter),
            ["null"] = typeof(NullReporter),
            ["compact"] = typeof(CompactReporter)
        };

        return services.AddNaturalApiReporting(defaultMap);
    }

    /// <summary>
    /// Registers reporter factory and reporters using the provided mapping.
    /// </summary>
    public static IServiceCollection AddNaturalApiReporting(this IServiceCollection services, IDictionary<string, Type> reporterMap)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (reporterMap == null) throw new ArgumentNullException(nameof(reporterMap));

        foreach (var kv in reporterMap)
        {
            var t = kv.Value;
            if (!services.Any(s => s.ServiceType == t))
            {
                services.AddSingleton(t);
            }
        }

        if (!services.Any(s => s.ServiceType == typeof(DefaultReporter)))
        {
            services.AddSingleton<DefaultReporter>();
        }

        if (!services.Any(s => s.ServiceType == typeof(INaturalReporter)))
        {
            services.AddSingleton<INaturalReporter>(provider => provider.GetRequiredService<DefaultReporter>());
        }

        services.AddSingleton<IReporterFactory>(provider => new ReporterFactory(provider, reporterMap));

        return services;
    }

    // ──────────────────────────────────────────────────────────────
    //  BACKWARD-COMPATIBLE OVERLOADS — thin wrappers over the builder
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers NaturalApi services with default configuration.
    /// </summary>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services)
    {
        return services.AddNaturalApi(_ => { });
    }

    /// <summary>
    /// Registers NaturalApi services with a NaturalApiConfiguration (legacy).
    /// </summary>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, NaturalApiConfiguration config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        return services.AddNaturalApi(builder =>
        {
            builder.BaseUrl = config.BaseUrl;
            builder.HttpClientName = config.HttpClientName;
            builder.AuthProvider = config.AuthProvider;
            builder.ReporterName = config.ReporterName;
        });
    }

    /// <summary>
    /// Registers NaturalApi services with a custom auth provider.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        return services.AddNaturalApi(builder => builder.WithAuth(authProvider));
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HttpClient instance.
    /// </summary>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, HttpClient httpClient)
    {
        if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));

        return services.AddNaturalApi(builder =>
        {
            builder.WithFactory(provider =>
            {
                var executor = new HttpClientExecutor(httpClient, null);
                var defaults = provider.GetService<IApiDefaultsProvider>();
                return defaults != null ? new Api(executor, defaults) : new Api(executor);
            });
        });
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HttpClient and auth provider.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, HttpClient httpClient, TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
        if (authProvider == null) throw new ArgumentNullException(nameof(authProvider));

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
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, string httpClientName)
    {
        return services.AddNaturalApi(builder => builder.WithHttpClient(httpClientName));
    }

    /// <summary>
    /// Registers NaturalApi services with a named HttpClient and auth provider.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, string httpClientName, TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        return services.AddNaturalApi(builder =>
        {
            builder.WithHttpClient(httpClientName);
            builder.WithAuth(authProvider);
        });
    }

    /// <summary>
    /// Registers NaturalApi services with a named HttpClient and auth provider factory.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TAuth>(this IServiceCollection services, string httpClientName, Func<IServiceProvider, TAuth> authProviderFactory)
        where TAuth : class, IApiAuthProvider
    {
        return services.AddNaturalApi(builder =>
        {
            builder.WithHttpClient(httpClientName);
            builder.WithAuth(sp => authProviderFactory(sp));
        });
    }

    /// <summary>
    /// Registers NaturalApi services with a custom API factory.
    /// </summary>
    public static IServiceCollection AddNaturalApi(this IServiceCollection services, Func<IServiceProvider, IApi> apiFactory)
    {
        return services.AddNaturalApi(builder => builder.WithFactory(apiFactory));
    }

    /// <summary>
    /// Registers NaturalApi services with a base URL and auth provider.
    /// </summary>
    public static IServiceCollection AddNaturalApiWithBaseUrl<TAuth>(
        this IServiceCollection services,
        string apiBaseUrl,
        TAuth authProvider)
        where TAuth : class, IApiAuthProvider
    {
        return services.AddNaturalApi(builder =>
        {
            builder.WithBaseUrl(apiBaseUrl);
            builder.WithAuth(authProvider);
        });
    }

    /// <summary>
    /// Registers NaturalApi services with a custom API factory and auth provider factory.
    /// </summary>
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
        return services.AddNaturalApi(builder => builder.WithFactory(apiFactory));
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HTTP executor type.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TExecutor>(this IServiceCollection services)
        where TExecutor : class, IHttpExecutor
    {
        return services.AddNaturalApi(builder => builder.WithExecutor<TExecutor>());
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HTTP executor factory.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TExecutor>(
        this IServiceCollection services,
        Func<IServiceProvider, TExecutor> executorFactory)
        where TExecutor : class, IHttpExecutor
    {
        return services.AddNaturalApi(builder => builder.WithExecutor(executorFactory));
    }

    /// <summary>
    /// Registers NaturalApi services with a custom HTTP executor and options.
    /// </summary>
    public static IServiceCollection AddNaturalApi<TExecutor, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions)
        where TExecutor : class, IHttpExecutor
        where TOptions : class, new()
    {
        var options = new TOptions();
        configureOptions(options);
        services.AddSingleton(options);

        return services.AddNaturalApi(builder =>
        {
            builder.WithExecutor(provider =>
            {
                var opts = provider.GetRequiredService<TOptions>();
                return Activator.CreateInstance(typeof(TExecutor), opts) as TExecutor
                    ?? throw new InvalidOperationException($"Failed to create {typeof(TExecutor).Name}");
            });
        });
    }

    // ──────────────────────────────────────────────────────────────
    //  CORE IMPLEMENTATION — all registration logic in one place
    // ──────────────────────────────────────────────────────────────

    private static IServiceCollection ApplyNaturalApiBuilder(this IServiceCollection services, NaturalApiBuilder builder)
    {
        // 1. Full API factory — skip everything else
        if (builder.ApiFactory != null)
        {
            services.AddScoped<IApi>(builder.ApiFactory);
            RegisterDefaultsIfMissing(services);
            return services;
        }

        // 2. Reporting
        if (!services.Any(s => s.ServiceType == typeof(IReporterFactory)))
        {
            services.AddNaturalApiReporting();
        }

        // 3. HttpClient
        var clientName = ResolveHttpClientName(builder);
        if (!string.IsNullOrEmpty(builder.BaseUrl))
        {
            services.AddHttpClient(clientName, client =>
            {
                client.BaseAddress = new Uri(builder.BaseUrl);
            });
        }
        else if (string.IsNullOrEmpty(builder.HttpClientName))
        {
            services.AddHttpClient(clientName);
        }

        // 4. Auth provider
        if (builder.AuthProviderFactory != null)
        {
            services.AddSingleton<IApiAuthProvider>(builder.AuthProviderFactory);
        }
        else if (builder.AuthProvider != null)
        {
            services.AddSingleton<IApiAuthProvider>(builder.AuthProvider);
        }

        // 5. Defaults provider
        var hasAuth = builder.AuthProvider != null || builder.AuthProviderFactory != null;
        if (hasAuth)
        {
            services.AddSingleton<IApiDefaultsProvider>(provider =>
            {
                var auth = provider.GetRequiredService<IApiAuthProvider>();
                return new DefaultApiDefaults(authProvider: auth);
            });
        }
        else
        {
            RegisterDefaultsIfMissing(services);
        }

        // 6. Custom executor
        if (builder.ExecutorFactory != null)
        {
            services.AddScoped<IHttpExecutor>(builder.ExecutorFactory);
        }
        else if (builder.ExecutorType != null)
        {
            services.AddScoped(typeof(IHttpExecutor), builder.ExecutorType);
        }

        // 7. IApi registration
        var capturedClientName = clientName;
        var capturedReporterName = builder.ReporterName;
        var hasCustomExecutor = builder.ExecutorFactory != null || builder.ExecutorType != null;

        services.AddScoped<IApi>(provider =>
        {
            var reporterFactory = provider.GetService<IReporterFactory>();
            INaturalReporter? reporter = null;

            if (!string.IsNullOrEmpty(capturedReporterName) && reporterFactory != null)
            {
                reporter = reporterFactory.Get(capturedReporterName!);
            }

            if (hasCustomExecutor)
            {
                var executor = provider.GetRequiredService<IHttpExecutor>();
                var defaults = provider.GetService<IApiDefaultsProvider>();
                return defaults != null ? new Api(executor, defaults, reporter) : new Api(executor);
            }

            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(capturedClientName);
            var defaultsProvider = provider.GetService<IApiDefaultsProvider>();

            if (defaultsProvider != null)
            {
                var reporterToUse = reporter ?? (defaultsProvider is DefaultApiDefaults dd ? dd.Reporter : null);

                IHttpExecutor exec = defaultsProvider.AuthProvider != null
                    ? new AuthenticatedHttpClientExecutor(httpClient, reporterToUse)
                    : new HttpClientExecutor(httpClient, reporterToUse);

                return new Api(exec, defaultsProvider, reporterToUse);
            }

            if (reporter != null)
            {
                return new Api(new HttpClientExecutor(httpClient, reporter));
            }

            return new Api(new HttpClientExecutor(httpClient, null));
        });

        return services;
    }

    private static string ResolveHttpClientName(NaturalApiBuilder builder)
    {
        if (!string.IsNullOrEmpty(builder.HttpClientName))
            return builder.HttpClientName!;
        if (!string.IsNullOrEmpty(builder.BaseUrl))
            return "NaturalApiClient";
        return "NaturalApi";
    }

    private static void RegisterDefaultsIfMissing(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(IApiDefaultsProvider)))
        {
            services.AddSingleton<IApiDefaultsProvider, DefaultApiDefaults>();
        }
    }
}
