using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiResultContextTests
{
    [TestMethod]
    public void Should_Throw_ArgumentNullException_When_Constructor_Is_Called_With_Null_Response()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ApiResultContext(null!, 0, new MockHttpExecutor()));
    }

    [TestMethod]
    public void Should_Return_StatusCode_When_Response_Is_Provided()
    {
        // Arrange
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        // Act
        var result = new ApiResultContext(response, 0, new MockHttpExecutor());

        // Assert
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Return_RawBody_When_Response_Is_Provided()
    {
        // Arrange
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"test\"}")
        };

        // Act
        var result = new ApiResultContext(response, 0, new MockHttpExecutor()); 

        // Assert
        Assert.IsNotNull(result.RawBody);
    }
}
