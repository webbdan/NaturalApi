using NaturalApi;
using NaturalApi.Reporter;

namespace NaturalApi.Tests;

/// <summary>
/// Mock authenticated HTTP executor for testing purposes.
/// Captures auth-related information without making real HTTP requests.
/// </summary>
internal class MockAuthenticatedHttpExecutor : IAuthenticatedHttpExecutor
{
    public ApiRequestSpec LastSpec { get; private set; } = null!;
    public IApiAuthProvider? LastAuthProvider { get; private set; }
    public string? LastUsername { get; private set; }
    public string? LastPassword { get; private set; }
    public bool LastSuppressAuth { get; private set; }
    public string? LastResolvedToken { get; private set; }

    private int _statusCode = 200;
    private string _responseBody = """{"message":"Mock response"}""";
    private IDictionary<string, string> _responseHeaders = new Dictionary<string, string>();

    private INaturalReporter _reporter = new NullReporter();
    public INaturalReporter Reporter { get => _reporter; set => _reporter = value ?? new NullReporter(); }

    public void SetupResponse(int statusCode, string responseBody, IDictionary<string, string>? headers = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
        _responseHeaders = headers ?? new Dictionary<string, string>();
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        return ExecuteAsync(spec, null, null, null, true).GetAwaiter().GetResult();
    }

    public async Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, IApiAuthProvider? authProvider, string? username, string? password, bool suppressAuth)
    {
        LastSpec = spec;
        LastAuthProvider = authProvider;
        LastUsername = username;
        LastPassword = password;
        LastSuppressAuth = suppressAuth;

        if (!suppressAuth && authProvider != null)
        {
            LastResolvedToken = await authProvider.GetAuthTokenAsync(username, password);
        }
        else
        {
            LastResolvedToken = null;
        }

        var response = new HttpResponseMessage((System.Net.HttpStatusCode)_statusCode)
        {
            Content = new StringContent(_responseBody)
        };

        foreach (var header in _responseHeaders)
        {
            response.Headers.Add(header.Key, header.Value);
        }

        return new MockApiResultContext(response, _responseBody, _responseHeaders, this);
    }
}
