using NaturalApi.Reporter;

namespace NaturalApi;

/// <summary>
/// Executes HTTP requests and returns result contexts.
/// All HTTP logic lives behind this interface.
/// </summary>
public interface IHttpExecutor
{
    /// <summary>
    /// Executes an HTTP request based on the provided specification.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <returns>Result context with response data and validation methods</returns>
    IApiResultContext Execute(ApiRequestSpec spec);

    /// <summary>
    /// Executes an HTTP request asynchronously based on the provided specification.
    /// </summary>
    /// <param name="spec">Request specification containing all request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result context with response data and validation methods</returns>
    Task<IApiResultContext> ExecuteAsync(ApiRequestSpec spec, CancellationToken cancellationToken = default);

    INaturalReporter Reporter { get; set; }
}
