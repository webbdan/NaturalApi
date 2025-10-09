using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace NaturalApi.Tests;

/// <summary>
/// Mock HTTP executor that can throw various HttpClient exceptions for testing.
/// </summary>
public class MockExceptionHttpExecutor : IHttpExecutor
{
    private readonly ExceptionType _exceptionType;
    private readonly string? _customMessage;

    public enum ExceptionType
    {
        HttpRequestException,
        SocketException,
        TaskCanceledException,
        AggregateException,
        InvalidOperationException,
        ArgumentException
    }

    public MockExceptionHttpExecutor(ExceptionType exceptionType, string? customMessage = null)
    {
        _exceptionType = exceptionType;
        _customMessage = customMessage;
    }

    public IApiResultContext Execute(ApiRequestSpec spec)
    {
        var exception = CreateException();
        throw new ApiExecutionException("Error during HTTP request execution", exception, spec);
    }

    private Exception CreateException()
    {
        return _exceptionType switch
        {
            ExceptionType.HttpRequestException => new HttpRequestException(
                _customMessage ?? "An error occurred while sending the request."),
            
            ExceptionType.SocketException => new SocketException(
                (int)SocketError.ConnectionRefused),
            
            ExceptionType.TaskCanceledException => new TaskCanceledException(
                _customMessage ?? "A task was canceled."),
            
            ExceptionType.AggregateException => new AggregateException(
                new HttpRequestException("Network error occurred"),
                new SocketException((int)SocketError.TimedOut)),
            
            ExceptionType.InvalidOperationException => new InvalidOperationException(
                _customMessage ?? "The operation is not valid for the current state."),
            
            ExceptionType.ArgumentException => new ArgumentException(
                _customMessage ?? "Invalid argument provided."),
            
            _ => new Exception("Unknown exception type")
        };
    }
}
