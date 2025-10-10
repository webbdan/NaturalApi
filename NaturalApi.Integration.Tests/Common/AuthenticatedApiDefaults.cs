using NaturalApi;

namespace NaturalApi.Integration.Tests.Common;

/// <summary>
/// Custom API defaults provider that includes authentication.
/// </summary>
public class AuthenticatedApiDefaults : IApiDefaultsProvider
{
    public IApiAuthProvider? AuthProvider { get; }
    public Uri? BaseUri { get; }
    public IDictionary<string, string> DefaultHeaders { get; }
    public TimeSpan Timeout { get; }

    public AuthenticatedApiDefaults(IApiAuthProvider authProvider, Uri? baseUri = null, IDictionary<string, string>? defaultHeaders = null, TimeSpan? timeout = null)
    {
        AuthProvider = authProvider;
        BaseUri = baseUri;
        DefaultHeaders = defaultHeaders ?? new Dictionary<string, string>();
        Timeout = timeout ?? TimeSpan.FromSeconds(30);
    }
}
