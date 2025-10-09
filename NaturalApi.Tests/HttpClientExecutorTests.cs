using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net;

namespace NaturalApi.Tests;

[TestClass]
public class HttpClientExecutorTests
{
    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Constructor_Is_Called_With_Null_HttpClient()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new HttpClientExecutor(null!));
    }

    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Execute_Is_Called_With_Null_Spec()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => executor.Execute(null!));
    }

    [TestMethod]
    public void Should_Return_ApiResultContext_When_Execute_Is_Called_With_Valid_Spec()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        var spec = new ApiRequestSpec(
            "https://httpbin.org/get",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);

        // Act
        var result = executor.Execute(spec);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IApiResultContext));
    }
}

