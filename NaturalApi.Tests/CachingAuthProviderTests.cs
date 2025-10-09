// AIModified:2025-10-09T07:22:36Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class CachingAuthProviderTests
{
    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_On_First_Call()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Same_Token_On_Subsequent_Calls()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token1 = await provider.GetAuthTokenAsync();
        var token2 = await provider.GetAuthTokenAsync();
        var token3 = await provider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token1);
        Assert.AreEqual("abc123", token2);
        Assert.AreEqual("abc123", token3);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();
        var username = "testuser";

        // Act
        var token = await provider.GetAuthTokenAsync(username);

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Null_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync(null);

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Empty_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync("");

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Token_With_Whitespace_Username()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync("   ");

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Be_Callable_Multiple_Times_Concurrently()
    {
        // Arrange
        var provider = new CachingAuthProvider();
        var tasks = new List<Task<string?>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(provider.GetAuthTokenAsync());
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10, results.Length);
        foreach (var result in results)
        {
            Assert.AreEqual("abc123", result);
        }
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Async_Operations()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token = await provider.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token);
    }

    [TestMethod]
    public void CachingAuthProvider_Should_Implement_IApiAuthProvider()
    {
        // Arrange & Act
        var provider = new CachingAuthProvider();

        // Assert
        Assert.IsInstanceOfType(provider, typeof(IApiAuthProvider));
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Return_Consistent_Token_Across_Multiple_Instances()
    {
        // Arrange
        var provider1 = new CachingAuthProvider();
        var provider2 = new CachingAuthProvider();

        // Act
        var token1 = await provider1.GetAuthTokenAsync();
        var token2 = await provider2.GetAuthTokenAsync();

        // Assert
        Assert.AreEqual("abc123", token1);
        Assert.AreEqual("abc123", token2);
    }

    [TestMethod]
    public async Task GetAuthTokenAsync_Should_Handle_Mixed_Username_Calls()
    {
        // Arrange
        var provider = new CachingAuthProvider();

        // Act
        var token1 = await provider.GetAuthTokenAsync();
        var token2 = await provider.GetAuthTokenAsync("user1");
        var token3 = await provider.GetAuthTokenAsync("user2");
        var token4 = await provider.GetAuthTokenAsync(null);

        // Assert
        Assert.AreEqual("abc123", token1);
        Assert.AreEqual("abc123", token2);
        Assert.AreEqual("abc123", token3);
        Assert.AreEqual("abc123", token4);
    }
}
