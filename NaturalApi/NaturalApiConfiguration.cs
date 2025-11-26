using System;

namespace NaturalApi;

/// <summary>
/// Configuration options for NaturalApi registration.
/// </summary>
public class NaturalApiConfiguration
{
    /// <summary>
    /// The base URL for the API. If not provided, absolute URLs must be used in For() calls.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The name of the HttpClient to use. If not provided, a default HttpClient will be used.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// The auth provider to use for authentication.
    /// </summary>
    public IApiAuthProvider? AuthProvider { get; set; }

    /// <summary>
    /// Optional reporter selection key. If provided, service registrations that support reporters
    /// will resolve a reporter instance from the registered reporter factory using this key.
    /// </summary>
    public string? ReporterName { get; set; }

    /// <summary>
    /// Creates a simple configuration with just a base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API</param>
    /// <returns>A configuration with the specified base URL</returns>
    public static NaturalApiConfiguration WithBaseUrl(string baseUrl)
    {
        return new NaturalApiConfiguration { BaseUrl = baseUrl };
    }

    /// <summary>
    /// Creates a configuration with a named HttpClient.
    /// </summary>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <returns>A configuration with the specified HttpClient name</returns>
    public static NaturalApiConfiguration WithHttpClient(string httpClientName)
    {
        return new NaturalApiConfiguration { HttpClientName = httpClientName };
    }

    /// <summary>
    /// Creates a configuration with an auth provider.
    /// </summary>
    /// <param name="authProvider">The auth provider to use</param>
    /// <returns>A configuration with the specified auth provider</returns>
    public static NaturalApiConfiguration WithAuth(IApiAuthProvider authProvider)
    {
        return new NaturalApiConfiguration { AuthProvider = authProvider };
    }

    /// <summary>
    /// Creates a configuration with both base URL and auth provider.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API</param>
    /// <param name="authProvider">The auth provider to use</param>
    /// <returns>A configuration with the specified base URL and auth provider</returns>
    public static NaturalApiConfiguration WithBaseUrlAndAuth(string baseUrl, IApiAuthProvider authProvider)
    {
        return new NaturalApiConfiguration { BaseUrl = baseUrl, AuthProvider = authProvider };
    }

    /// <summary>
    /// Creates a configuration with both HttpClient name and auth provider.
    /// </summary>
    /// <param name="httpClientName">The name of the HttpClient to use</param>
    /// <param name="authProvider">The auth provider to use</param>
    /// <returns>A configuration with the specified HttpClient name and auth provider</returns>
    public static NaturalApiConfiguration WithHttpClientAndAuth(string httpClientName, IApiAuthProvider authProvider)
    {
        return new NaturalApiConfiguration { HttpClientName = httpClientName, AuthProvider = authProvider };
    }
}
