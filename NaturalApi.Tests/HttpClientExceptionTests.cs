using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net;
using System.Net.Sockets;

namespace NaturalApi.Tests;

[TestClass]
public class HttpClientExceptionTests
{
    private Api _api = null!;

    [TestInitialize]
    public void Setup()
    {
        // We'll create the API with different executors for each test
    }

    #region ApiExecutionException Tests - HttpRequestException Wrapping

    [TestMethod]
    public void Should_Propagate_ApiExecutionException_When_Network_Error_Occurs()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/users", exception.Endpoint);
        Assert.AreEqual(HttpMethod.Get, exception.Method);
        Assert.IsInstanceOfType(exception.InnerException, typeof(HttpRequestException));
        Assert.AreEqual("An error occurred while sending the request.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Propagate_ApiExecutionException_With_Custom_Message()
    {
        // Arrange
        var customMessage = "Connection to server failed";
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException, customMessage);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("https://api.example.com/users", exception.Endpoint);
        Assert.IsInstanceOfType(exception.InnerException, typeof(HttpRequestException));
        Assert.AreEqual(customMessage, exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_For_Post_Request()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Post(new { name = "John", email = "john@example.com" }));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_For_Put_Request()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/1")
                .Put(new { name = "John Updated" }));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_For_Delete_Request()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/1")
                .Delete());

        Assert.IsNotNull(exception);
    }

    #endregion

    #region SocketException Tests

    [TestMethod]
    public void Should_Propagate_SocketException_When_Connection_Refused()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.SocketException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception.InnerException, typeof(SocketException));
        Assert.AreEqual((int)SocketError.ConnectionRefused, ((SocketException)exception.InnerException!).ErrorCode);
    }

    [TestMethod]
    public void Should_Propagate_SocketException_For_All_Http_Methods()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.SocketException);
        _api = new Api(executor);

        // Act & Assert - GET
        Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users").Get());

        // Act & Assert - POST
        Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Post(new { name = "Test" }));

        // Act & Assert - PUT
        Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/1")
                .Put(new { name = "Test" }));

        // Act & Assert - DELETE
        Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/1").Delete());
    }

    #endregion

    #region TaskCanceledException Tests

    [TestMethod]
    public void Should_Propagate_TaskCanceledException_When_Request_Is_Canceled()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.TaskCanceledException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("A task was canceled.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Propagate_TaskCanceledException_With_Custom_Message()
    {
        // Arrange
        var customMessage = "Request was canceled due to timeout";
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.TaskCanceledException, customMessage);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual(customMessage, exception.InnerException?.Message);
    }

    #endregion

    #region AggregateException Tests

    [TestMethod]
    public void Should_Propagate_AggregateException_When_Multiple_Exceptions_Occur()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.AggregateException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception.InnerException, typeof(AggregateException));
        var aggregateException = (AggregateException)exception.InnerException!;
        Assert.AreEqual(2, aggregateException.InnerExceptions.Count);
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[0], typeof(HttpRequestException));
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[1], typeof(SocketException));
    }

    #endregion

    #region Other Exception Tests

    [TestMethod]
    public void Should_Propagate_InvalidOperationException_When_Invalid_State()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.InvalidOperationException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("The operation is not valid for the current state.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Propagate_ArgumentException_When_Invalid_Argument()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.ArgumentException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get());

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("Invalid argument provided.", exception.InnerException?.Message);
    }

    #endregion

    #region Exception Handling with Different Request Configurations

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Headers()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithHeader("Authorization", "Bearer token123")
                .WithHeader("Content-Type", "application/json")
                .Get());

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Query_Parameters()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithQueryParam("page", 1)
                .WithQueryParam("limit", 10)
                .Get());

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Path_Parameters()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 123)
                .Get());

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Timeout()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithTimeout(TimeSpan.FromSeconds(30))
                .Get());

        Assert.IsNotNull(exception);
    }

    #endregion

    #region Exception Handling with Complex Request Bodies

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Complex_Object_Body()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);
        var complexObject = new
        {
            user = new
            {
                name = "John Doe",
                email = "john@example.com",
                address = new
                {
                    street = "123 Main St",
                    city = "Anytown",
                    zipCode = "12345"
                }
            },
            preferences = new[] { "email", "sms" }
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Post(complexObject));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Array_Body()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);
        var arrayBody = new[]
        {
            new { id = 1, name = "Item 1" },
            new { id = 2, name = "Item 2" },
            new { id = 3, name = "Item 3" }
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/items")
                .Post(arrayBody));

        Assert.IsNotNull(exception);
    }

    #endregion

    #region DSL Exception Tests - Complete Flow with ShouldReturn

    [TestMethod]
    public void Should_Propagate_HttpRequestException_During_ShouldReturn_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert - Exception should be thrown during the Get() call, not during ShouldReturn
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get()
                .ShouldReturn<UserResponse>(status: 200));

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("An error occurred while sending the request.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Propagate_SocketException_During_ShouldReturn_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.SocketException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/123")
                .Get()
                .ShouldReturn<UserResponse>(
                    status: 200,
                    body => body.Id == 123 && !string.IsNullOrEmpty(body.Name)));

        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception.InnerException, typeof(SocketException));
        Assert.AreEqual((int)SocketError.ConnectionRefused, ((SocketException)exception.InnerException!).ErrorCode);
    }

    [TestMethod]
    public void Should_Propagate_TaskCanceledException_During_ShouldReturn_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.TaskCanceledException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithTimeout(TimeSpan.FromSeconds(5))
                .Get()
                .ShouldReturn<UserResponse[]>(
                    status: 200,
                    body => body.Length > 0,
                    headers => headers.ContainsKey("Content-Type")));

        Assert.IsNotNull(exception);
        Assert.AreEqual("Error during HTTP request execution", exception.Message);
        Assert.AreEqual("A task was canceled.", exception.InnerException?.Message);
    }

    [TestMethod]
    public void Should_Propagate_AggregateException_During_ShouldReturn_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.AggregateException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithHeader("Authorization", "Bearer token123")
                .Get()
                .ShouldReturn<UserResponse[]>(status: 200));

        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception.InnerException, typeof(AggregateException));
        var aggregateException = (AggregateException)exception.InnerException!;
        Assert.AreEqual(2, aggregateException.InnerExceptions.Count);
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[0], typeof(HttpRequestException));
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[1], typeof(SocketException));
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_During_Complex_DSL_Chain()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert - Complex DSL chain with multiple configurations
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 123)
                .WithQueryParam("include", "profile")
                .WithQueryParam("fields", "name,email")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NaturalApi/1.0")
                .WithTimeout(TimeSpan.FromSeconds(30))
                .Get()
                .ShouldReturn<UserResponse>(
                    status: 200,
                    body => body.Id == 123 && !string.IsNullOrEmpty(body.Name),
                    headers => headers.ContainsKey("Content-Type") && headers["Content-Type"].Contains("application/json")));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_During_Post_With_ShouldReturn()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);
        var userData = new { name = "John Doe", email = "john@example.com" };

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .WithHeader("Content-Type", "application/json")
                .Post(userData)
                .ShouldReturn<CreatedUserResponse>(
                    status: 201,
                    body => body.Id > 0 && body.Name == "John Doe"));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_During_Put_With_ShouldReturn()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);
        var updateData = new { name = "John Updated", email = "john.updated@example.com" };

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 123)
                .WithHeader("Content-Type", "application/json")
                .Put(updateData)
                .ShouldReturn<UserResponse>(
                    status: 200,
                    body => body.Id == 123 && body.Name == "John Updated"));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_During_Delete_With_ShouldReturn()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 123)
                .WithHeader("Authorization", "Bearer token123")
                .Delete()
                .ShouldReturn(status: 204));

        Assert.IsNotNull(exception);
    }

    #endregion

    #region DSL Exception Tests - Then() Chaining

    [TestMethod]
    public void Should_Propagate_HttpRequestException_During_Then_Chaining()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get()
                .ShouldReturn<UserResponse[]>(status: 200)
                .Then(result =>
                {
                    // This should never execute due to exception
                    Assert.Fail("This should not be reached due to exception");
                }));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_SocketException_During_Complex_Then_Chaining()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.SocketException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users/{id}")
                .WithPathParam("id", 123)
                .WithQueryParam("include", "profile")
                .Get()
                .ShouldReturn<UserResponse>(
                    status: 200,
                    body => body.Id == 123)
                .Then(result =>
                {
                    // This should never execute due to exception
                    Assert.Fail("This should not be reached due to exception");
                }));

        Assert.IsNotNull(exception);
    }

    #endregion

    #region DSL Exception Tests - Multiple Validation Scenarios

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Status_Only_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get()
                .ShouldReturn(200));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Body_Only_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get()
                .ShouldReturn<UserResponse[]>(body => body.Length > 0));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Headers_Only_Validation()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
            _api.For("https://api.example.com/users")
                .Get()
                .ShouldReturn<UserResponse[]>(headers: headers => headers.ContainsKey("Content-Type")));

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Should_Propagate_HttpRequestException_With_Generic_ShouldReturn()
    {
        // Arrange
        var executor = new MockExceptionHttpExecutor(MockExceptionHttpExecutor.ExceptionType.HttpRequestException);
        _api = new Api(executor);

        // Act & Assert
        var exception = Assert.ThrowsException<ApiExecutionException>(() =>
        {
            var result = _api.For("https://api.example.com/users")
                .Get()
                .ShouldReturn<UserResponse[]>();
            
            // This should never be reached due to exception
            Assert.Fail("This should not be reached due to exception");
        });

        Assert.IsNotNull(exception);
    }

    #endregion

    #region Test Data Classes

    private class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class CreatedUserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
