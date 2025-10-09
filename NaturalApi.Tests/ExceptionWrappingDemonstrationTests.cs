using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net;
using System.Net.Sockets;

namespace NaturalApi.Tests;

/// <summary>
/// Tests that demonstrate the improved exception wrapping behavior.
/// Shows the difference between raw HttpClient exceptions and wrapped exceptions with context.
/// </summary>
[TestClass]
public class ExceptionWrappingDemonstrationTests
{
    private Api _api = null!;

    [TestInitialize]
    public void Setup()
    {
        // We'll create the API with different executors for each test
    }

    #region Demonstration Tests - Before vs After Exception Handling

    [TestMethod]
    public void Should_Demonstrate_Improved_Exception_Context_For_HttpRequestException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithHeader("Authorization", "Bearer token123")
                .WithHeader("Content-Type", "application/json")
                .WithQueryParam("page", 1)
                .WithQueryParam("limit", 10)
                .WithPathParam("id", 123)
                .WithTimeout(TimeSpan.FromSeconds(30))
                .Post(new { name = "John Doe", email = "john@example.com" }));

        // Verify the wrapped exception contains full context
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/users", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Post, exception.Method);
        Assert.AreEqual(2, exception.Headers.Count);
        Assert.IsTrue(exception.Headers.ContainsKey("Authorization"));
        Assert.IsTrue(exception.Headers.ContainsKey("Content-Type"));
        Assert.AreEqual(2, exception.QueryParams.Count);
        Assert.AreEqual(1, exception.PathParams.Count);
        Assert.IsNotNull(exception.Body);
        Assert.AreEqual(TimeSpan.FromSeconds(30), exception.Timeout);
        
        // Verify inner exception is preserved
        Assert.IsInstanceOfType(exception.InnerException, typeof(HttpRequestException));
        Assert.AreEqual("An error occurred while sending the request.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Demonstrate_Improved_Exception_Context_For_SocketException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.SocketException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 456)
                .WithHeader("User-Agent", "NaturalApi/1.0")
                .WithQueryParam("include", "profile")
                .Get());

        // Verify the wrapped exception contains full context
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/users/{id}", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Get, exception.Method);
        Assert.AreEqual(1, exception.Headers.Count);
        Assert.IsTrue(exception.Headers.ContainsKey("User-Agent"));
        Assert.AreEqual(1, exception.QueryParams.Count);
        Assert.AreEqual(1, exception.PathParams.Count);
        Assert.AreEqual(456, exception.PathParams["id"]);
        
        // Verify inner exception is preserved
        Assert.IsInstanceOfType(exception.InnerException, typeof(SocketException));
        Assert.AreEqual((int)SocketError.ConnectionRefused, ((SocketException)exception.InnerException!).ErrorCode);
    }

    [TestMethod]
    public void Should_Demonstrate_Improved_Exception_Context_For_TaskCanceledException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.TaskCanceledException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/data")
                .WithTimeout(TimeSpan.FromSeconds(5))
                .WithHeader("Accept", "application/json")
                .Get());

        // Verify the wrapped exception contains full context
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/data", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Get, exception.Method);
        Assert.AreEqual(1, exception.Headers.Count);
        Assert.IsTrue(exception.Headers.ContainsKey("Accept"));
        Assert.AreEqual(TimeSpan.FromSeconds(5), exception.Timeout);
        
        // Verify inner exception is preserved
        Assert.IsInstanceOfType(exception.InnerException, typeof(TaskCanceledException));
        Assert.AreEqual("A task was canceled.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Demonstrate_Improved_Exception_Context_For_AggregateException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.AggregateException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/complex-endpoint")
                .WithHeader("Authorization", "Bearer token456")
                .WithHeader("X-Request-ID", "req-789")
                .WithQueryParam("filter", "active")
                .WithQueryParam("sort", "name")
                .WithPathParam("tenant", "acme")
                .WithPathParam("version", "v2")
                .Put(new { status = "updated", timestamp = DateTime.UtcNow }));

        // Verify the wrapped exception contains full context
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/complex-endpoint", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Put, exception.Method);
        Assert.AreEqual(2, exception.Headers.Count);
        Assert.IsTrue(exception.Headers.ContainsKey("Authorization"));
        Assert.IsTrue(exception.Headers.ContainsKey("X-Request-ID"));
        Assert.AreEqual(2, exception.QueryParams.Count);
        Assert.AreEqual(2, exception.PathParams.Count);
        Assert.IsNotNull(exception.Body);
        
        // Verify inner exception is preserved
        Assert.IsInstanceOfType(exception.InnerException, typeof(AggregateException));
        var aggregateException = (AggregateException)exception.InnerException!;
        Assert.AreEqual(2, aggregateException.InnerExceptions.Count);
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[0], typeof(HttpRequestException));
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[1], typeof(SocketException));
    }

    #endregion

    #region User-Friendly Error Message Tests

    [TestMethod]
    public void Should_Provide_User_Friendly_Error_Message_For_HttpRequestException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Post(new { name = "Test User" }));

        // Verify user-friendly message
        var friendlyMessage = exception.GetUserFriendlyMessage();
        Assert.IsTrue(friendlyMessage.Contains("[POST] https://api.example.com/users failed"));
        Assert.IsTrue(friendlyMessage.Contains("Network connection failed"));
    }

    [TestMethod]
    public void Should_Provide_User_Friendly_Error_Message_For_TaskCanceledException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.TaskCanceledException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/data")
                .WithTimeout(TimeSpan.FromSeconds(1))
                .Get());

        // Verify user-friendly message
        var friendlyMessage = exception.GetUserFriendlyMessage();
        Assert.IsTrue(friendlyMessage.Contains("[GET] https://api.example.com/data failed"));
        Assert.IsTrue(friendlyMessage.Contains("Request timed out"));
    }

    [TestMethod]
    public void Should_Provide_User_Friendly_Error_Message_For_SocketException()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.SocketException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/123")
                .Put(new { name = "Updated User" }));

        // Verify user-friendly message
        var friendlyMessage = exception.GetUserFriendlyMessage();
        Assert.IsTrue(friendlyMessage.Contains("[PUT] https://api.example.com/users/123 failed"));
        Assert.IsTrue(friendlyMessage.Contains("Connection to server failed"));
    }

    #endregion

    #region Detailed Exception Information Tests

    [TestMethod]
    public void Should_Provide_Detailed_Exception_Information_With_ToString()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithHeader("Authorization", "Bearer token123")
                .WithHeader("Content-Type", "application/json")
                .WithQueryParam("page", 1)
                .WithQueryParam("limit", 10)
                .WithPathParam("id", 123)
                .WithTimeout(TimeSpan.FromSeconds(30))
                .Post(new { name = "John Doe", email = "john@example.com" }));

        // Verify detailed string representation
        var detailedInfo = exception.ToString();
        Assert.IsTrue(detailedInfo.Contains("[POST] https://api.example.com/users failed"));
        Assert.IsTrue(detailedInfo.Contains("Inner Exception: HttpRequestException"));
        Assert.IsTrue(detailedInfo.Contains("Headers:"));
        Assert.IsTrue(detailedInfo.Contains("Authorization=Bearer token123"));
        Assert.IsTrue(detailedInfo.Contains("Content-Type=application/json"));
        Assert.IsTrue(detailedInfo.Contains("Query Params:"));
        Assert.IsTrue(detailedInfo.Contains("page=1"));
        Assert.IsTrue(detailedInfo.Contains("limit=10"));
        Assert.IsTrue(detailedInfo.Contains("Path Params:"));
        Assert.IsTrue(detailedInfo.Contains("id=123"));
        Assert.IsTrue(detailedInfo.Contains("Body:"));
        Assert.IsTrue(detailedInfo.Contains("Timeout: 30s"));
    }

    [TestMethod]
    public void Should_Handle_Exception_Without_Optional_Parameters()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert - Simple GET request without optional parameters
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/health")
                .Get());

        // Verify basic context is still captured
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/health", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Get, exception.Method);
        Assert.AreEqual(0, exception.Headers.Count);
        Assert.AreEqual(0, exception.QueryParams.Count);
        Assert.AreEqual(0, exception.PathParams.Count);
        Assert.IsNull(exception.Body);
        Assert.IsNull(exception.Timeout);
        
        // Verify detailed string representation handles empty collections
        var detailedInfo = exception.ToString();
        Assert.IsTrue(detailedInfo.Contains("[GET] https://api.example.com/health failed"));
        Assert.IsTrue(detailedInfo.Contains("Inner Exception: HttpRequestException"));
    }

    #endregion

    #region Comparison Tests - Before vs After

    [TestMethod]
    public void Should_Demonstrate_Exception_Context_Improvement()
    {
        // This test demonstrates the improvement in exception handling
        // by showing what information is now available vs. what was available before

        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 123)
                .WithHeader("Authorization", "Bearer token123")
                .WithQueryParam("include", "profile")
                .WithTimeout(TimeSpan.FromSeconds(30))
                .Put(new { name = "Updated User" }));

        // BEFORE: Raw HttpClient exception would only tell us:
        // "HttpRequestException: An error occurred while sending the request."
        // 
        // AFTER: ApiExecutionException tells us the complete story:
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/users/{id}", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Put, exception.Method);
        Assert.AreEqual(1, exception.Headers.Count);
        Assert.AreEqual("Bearer token123", exception.Headers["Authorization"]);
        Assert.AreEqual(1, exception.QueryParams.Count);
        Assert.AreEqual("profile", exception.QueryParams["include"]);
        Assert.AreEqual(1, exception.PathParams.Count);
        Assert.AreEqual(123, exception.PathParams["id"]);
        Assert.IsNotNull(exception.Body);
        Assert.AreEqual(TimeSpan.FromSeconds(30), exception.Timeout);
        
        // The inner exception is still available for technical details
        Assert.IsInstanceOfType(exception.InnerException, typeof(HttpRequestException));
        Assert.AreEqual("An error occurred while sending the request.", exception.InnerException?.Message);
    }

    #endregion
}
