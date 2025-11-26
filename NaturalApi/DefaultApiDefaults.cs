using NaturalApi.Reporter;

namespace NaturalApi;

/// <summary>
/// Default implementation of IApiDefaultsProvider.
/// Provides sensible defaults for API configuration.
/// </summary>
public class DefaultApiDefaults : IApiDefaultsProvider
{
    /// <summary>
    /// Initializes a new instance of DefaultApiDefaults with default values.
    /// </summary>
    public DefaultApiDefaults() : this(null, null, TimeSpan.FromSeconds(30), null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of DefaultApiDefaults with specified values.
    /// </summary>
    /// <param name="baseUri">Base URI for all requests</param>
    /// <param name="defaultHeaders">Default headers to be added to all requests</param>
    /// <param name="timeout">Default timeout for requests</param>
    /// <param name="authProvider">Authentication provider for automatic token resolution</param>
    /// <param name="reporter">Optional reporter to use for this defaults provider</param>
    public DefaultApiDefaults(
        Uri? baseUri = null,
        IDictionary<string, string>? defaultHeaders = null,
        TimeSpan? timeout = null,
        IApiAuthProvider? authProvider = null,
        NaturalApi.Reporter.INaturalReporter? reporter = null)
    {
        BaseUri = baseUri;
        DefaultHeaders = defaultHeaders ?? new Dictionary<string, string>();
        Timeout = timeout ?? TimeSpan.FromSeconds(30);
        AuthProvider = authProvider;
        Reporter = reporter;
    }

    /// <summary>
    /// Base URI for all requests. Can be null if not specified.
    /// </summary>
    public Uri? BaseUri { get; }

    /// <summary>
    /// Default headers to be added to all requests.
    /// </summary>
    public IDictionary<string, string> DefaultHeaders { get; }

    /// <summary>
    /// Default timeout for requests.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Authentication provider for automatic token resolution.
    /// Can be null if no authentication is configured.
    /// </summary>
    public IApiAuthProvider? AuthProvider { get; }

    /// <summary>
    /// Optional reporter to be used for this set of APIs (config-level).
    /// </summary>
    public NaturalApi.Reporter.INaturalReporter? Reporter { get; }
}
