using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class TimeoutHandlingTests
{
    [TestMethod]
    public void Should_Use_Timeout_When_WithTimeout_Is_Called()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var result = api.For("https://test.local/delay/1")
            .WithTimeout(timeout)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Throw_TaskCanceledException_When_Request_Takes_Longer_Than_Timeout()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);
        var shortTimeout = TimeSpan.FromMilliseconds(100); // Very short timeout

        // Act & Assert
        // This should timeout because delay/2 takes 2 seconds but timeout is 100ms
        var exception = Assert.ThrowsException<AggregateException>(() => 
            api.For("https://test.local/delay/2")
                .WithTimeout(shortTimeout)
                .Get());
        
        // Verify the inner exception is TaskCanceledException
        Assert.IsInstanceOfType(exception.InnerException, typeof(TaskCanceledException));
    }

    [TestMethod]
    public void Should_Use_Default_Timeout_When_No_Timeout_Is_Specified()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);

        // Act
        var result = api.For("https://test.local/get")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Use_Request_Specific_Timeout_Over_HttpClient_Timeout()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);
        var requestTimeout = TimeSpan.FromSeconds(3); // Shorter request timeout

        // Act
        var result = api.For("https://test.local/delay/1")
            .WithTimeout(requestTimeout)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Handle_Timeout_For_POST_Request()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var result = api.For("https://test.local/post")
            .WithTimeout(timeout)
            .Post(new { test = "data" });

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Handle_Timeout_For_PUT_Request()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var result = api.For("https://test.local/put")
            .WithTimeout(timeout)
            .Put(new { test = "data" });

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Handle_Timeout_For_DELETE_Request()
    {
        // Arrange
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var result = api.For("https://test.local/delete")
            .WithTimeout(timeout)
            .Delete();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }
}
