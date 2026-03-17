using NaturalApi.Reporter;

namespace NaturalApi;

/// <summary>
/// Builder for configuring NaturalApi service registration.
/// Used with services.AddNaturalApi(builder => { ... }) as the single DI entry point.
/// </summary>
public class NaturalApiBuilder
{
    /// <summary>
    /// The base URL for the API. If not provided, absolute URLs must be used in For() calls.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The name of the HttpClient to use from IHttpClientFactory.
    /// If not provided, a default named client "NaturalApi" is registered.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// The auth provider instance for authentication.
    /// </summary>
    public IApiAuthProvider? AuthProvider { get; set; }

    /// <summary>
    /// Factory function for creating the auth provider from DI.
    /// Takes precedence over AuthProvider if both are set.
    /// </summary>
    public Func<IServiceProvider, IApiAuthProvider>? AuthProviderFactory { get; set; }

    /// <summary>
    /// Optional reporter selection key. Resolves a reporter from the registered IReporterFactory.
    /// </summary>
    public string? ReporterName { get; set; }

    /// <summary>
    /// Optional custom executor type. When set, this type is registered as IHttpExecutor
    /// instead of the default HttpClientExecutor.
    /// </summary>
    public Type? ExecutorType { get; set; }

    /// <summary>
    /// Optional factory for creating a custom IHttpExecutor.
    /// Takes precedence over ExecutorType if both are set.
    /// </summary>
    public Func<IServiceProvider, IHttpExecutor>? ExecutorFactory { get; set; }

    /// <summary>
    /// Optional factory for creating the entire IApi instance.
    /// When set, all other configuration is ignored — you have full control.
    /// </summary>
    public Func<IServiceProvider, IApi>? ApiFactory { get; set; }

    /// <summary>
    /// Sets the base URL for all requests.
    /// </summary>
    public NaturalApiBuilder WithBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Sets the named HttpClient to use from IHttpClientFactory.
    /// </summary>
    public NaturalApiBuilder WithHttpClient(string httpClientName)
    {
        HttpClientName = httpClientName;
        return this;
    }

    /// <summary>
    /// Sets the auth provider instance.
    /// </summary>
    public NaturalApiBuilder WithAuth(IApiAuthProvider authProvider)
    {
        AuthProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        return this;
    }

    /// <summary>
    /// Sets the auth provider factory for DI-resolved auth providers.
    /// </summary>
    public NaturalApiBuilder WithAuth(Func<IServiceProvider, IApiAuthProvider> authProviderFactory)
    {
        AuthProviderFactory = authProviderFactory ?? throw new ArgumentNullException(nameof(authProviderFactory));
        return this;
    }

    /// <summary>
    /// Sets the reporter name for configuration-driven reporter selection.
    /// </summary>
    public NaturalApiBuilder WithReporter(string reporterName)
    {
        ReporterName = reporterName;
        return this;
    }

    /// <summary>
    /// Uses a custom IHttpExecutor type instead of the default HttpClientExecutor.
    /// </summary>
    public NaturalApiBuilder WithExecutor<TExecutor>() where TExecutor : class, IHttpExecutor
    {
        ExecutorType = typeof(TExecutor);
        return this;
    }

    /// <summary>
    /// Uses a custom IHttpExecutor factory.
    /// </summary>
    public NaturalApiBuilder WithExecutor(Func<IServiceProvider, IHttpExecutor> executorFactory)
    {
        ExecutorFactory = executorFactory ?? throw new ArgumentNullException(nameof(executorFactory));
        return this;
    }

    /// <summary>
    /// Provides a fully custom IApi factory. All other configuration is ignored.
    /// </summary>
    public NaturalApiBuilder WithFactory(Func<IServiceProvider, IApi> apiFactory)
    {
        ApiFactory = apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));
        return this;
    }
}
