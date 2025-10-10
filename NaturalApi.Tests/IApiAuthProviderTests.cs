using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class IApiAuthProviderTests
{
    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_When_Username_Provided()
    {
        // Arrange
        var authProvider = new TestAuthProvider("test-token");
        var username = "testuser";

        // Act
        var token = await authProvider.GetAuthTokenAsync(username);

        // Assert
        Assert.AreEqual("test-token", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_When_Username_Is_Null()
    {
        // Arrange
        var authProvider = new TestAuthProvider("default-token");

        // Act
        var token = await authProvider.GetAuthTokenAsync(null);

        // Assert
        Assert.AreEqual("default-token", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Null_When_No_Auth_Needed()
    {
        // Arrange
        var authProvider = new TestAuthProvider(null);

        // Act
        var token = await authProvider.GetAuthTokenAsync();

        // Assert
        Assert.IsNull(token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Empty_Username()
    {
        // Arrange
        var authProvider = new TestAuthProvider("empty-username-token");
        var emptyUsername = "";

        // Act
        var token = await authProvider.GetAuthTokenAsync(emptyUsername);

        // Assert
        Assert.AreEqual("empty-username-token", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Whitespace_Username()
    {
        // Arrange
        var authProvider = new TestAuthProvider("whitespace-username-token");
        var whitespaceUsername = "   ";

        // Act
        var token = await authProvider.GetAuthTokenAsync(whitespaceUsername);

        // Assert
        Assert.AreEqual("whitespace-username-token", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Be_Callable_Multiple_Times()
    {
        // Arrange
        var authProvider = new TestAuthProvider("multi-call-token");

        // Act
        var token1 = await authProvider.GetAuthTokenAsync();
        var token2 = await authProvider.GetAuthTokenAsync("user1");
        var token3 = await authProvider.GetAuthTokenAsync("user2");

        // Assert
        Assert.AreEqual("multi-call-token", token1);
        Assert.AreEqual("multi-call-token", token2);
        Assert.AreEqual("multi-call-token", token3);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Async_Operations()
    {
        // Arrange
        var authProvider = new AsyncTestAuthProvider("async-token", 100);

        // Act
        var token = await authProvider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("async-token", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Exceptions_Gracefully()
    {
        // Arrange
        var authProvider = new ExceptionThrowingAuthProvider();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => authProvider.GetAuthTokenAsync());
    }

    /// <summary>
    /// Test implementation of IApiAuthProvider for testing purposes.
    /// </summary>
    private class TestAuthProvider : IApiAuthProvider
    {
        private readonly string? _token;

        public TestAuthProvider(string? token)
        {
            _token = token;
        }

        public Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
        {
            return Task.FromResult(_token);
        }
    }

    /// <summary>
    /// Async test implementation of IApiAuthProvider for testing async behavior.
    /// </summary>
    private class AsyncTestAuthProvider : IApiAuthProvider
    {
        private readonly string _token;
        private readonly int _delayMs;

        public AsyncTestAuthProvider(string token, int delayMs)
        {
            _token = token;
            _delayMs = delayMs;
        }

        public async Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
        {
            await Task.Delay(_delayMs);
            return _token;
        }
    }

    /// <summary>
    /// Exception-throwing implementation of IApiAuthProvider for testing error handling.
    /// </summary>
    private class ExceptionThrowingAuthProvider : IApiAuthProvider
    {
        public Task<string?> GetAuthTokenAsync(string? username = null, string? password = null)
        {
            throw new InvalidOperationException("Auth provider error");
        }
    }
}
