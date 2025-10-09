using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class ApiAssertionExceptionTests
{
    [TestMethod]
    public void Should_Create_Exception_With_All_Properties_When_Constructor_Is_Called()
    {
        // Arrange
        var message = "Status code assertion failed";
        var failedExpectation = "Expected status 200";
        var actualValues = "Actual status 404";
        var endpoint = "/users";
        var httpVerb = "GET";
        var responseBodySnippet = "{\"error\":\"Not Found\"}";

        // Act
        var exception = new ApiAssertionException(
            message,
            failedExpectation,
            actualValues,
            endpoint,
            httpVerb,
            responseBodySnippet);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(failedExpectation, exception.FailedExpectation);
        Assert.AreEqual(actualValues, exception.ActualValues);
        Assert.AreEqual(endpoint, exception.Endpoint);
        Assert.AreEqual(httpVerb, exception.HttpVerb);
        Assert.AreEqual(responseBodySnippet, exception.ResponseBodySnippet);
    }

    [TestMethod]
    public void Should_Create_Exception_Without_Response_Body_Snippet_When_Constructor_Is_Called_With_Null()
    {
        // Arrange
        var message = "Status code assertion failed";
        var failedExpectation = "Expected status 200";
        var actualValues = "Actual status 404";
        var endpoint = "/users";
        var httpVerb = "GET";

        // Act
        var exception = new ApiAssertionException(
            message,
            failedExpectation,
            actualValues,
            endpoint,
            httpVerb,
            null);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(failedExpectation, exception.FailedExpectation);
        Assert.AreEqual(actualValues, exception.ActualValues);
        Assert.AreEqual(endpoint, exception.Endpoint);
        Assert.AreEqual(httpVerb, exception.HttpVerb);
        Assert.IsNull(exception.ResponseBodySnippet);
    }

    [TestMethod]
    public void Should_Create_Exception_Without_Response_Body_Snippet_When_Constructor_Is_Called_Without_Snippet_Parameter()
    {
        // Arrange
        var message = "Status code assertion failed";
        var failedExpectation = "Expected status 200";
        var actualValues = "Actual status 404";
        var endpoint = "/users";
        var httpVerb = "GET";

        // Act
        var exception = new ApiAssertionException(
            message,
            failedExpectation,
            actualValues,
            endpoint,
            httpVerb);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(failedExpectation, exception.FailedExpectation);
        Assert.AreEqual(actualValues, exception.ActualValues);
        Assert.AreEqual(endpoint, exception.Endpoint);
        Assert.AreEqual(httpVerb, exception.HttpVerb);
        Assert.IsNull(exception.ResponseBodySnippet);
    }

    [TestMethod]
    public void Should_Inherit_From_Exception()
    {
        // Arrange
        var message = "Test exception";
        var failedExpectation = "Expected something";
        var actualValues = "Actual something";
        var endpoint = "/test";
        var httpVerb = "GET";

        // Act
        var exception = new ApiAssertionException(
            message,
            failedExpectation,
            actualValues,
            endpoint,
            httpVerb);

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void Should_Handle_Empty_Strings_When_Constructor_Is_Called()
    {
        // Arrange
        var message = "";
        var failedExpectation = "";
        var actualValues = "";
        var endpoint = "";
        var httpVerb = "";

        // Act
        var exception = new ApiAssertionException(
            message,
            failedExpectation,
            actualValues,
            endpoint,
            httpVerb);

        // Assert
        Assert.AreEqual("", exception.Message);
        Assert.AreEqual("", exception.FailedExpectation);
        Assert.AreEqual("", exception.ActualValues);
        Assert.AreEqual("", exception.Endpoint);
        Assert.AreEqual("", exception.HttpVerb);
        Assert.IsNull(exception.ResponseBodySnippet);
    }

    [TestMethod]
    public void Should_Handle_Long_Strings_When_Constructor_Is_Called()
    {
        // Arrange
        var longMessage = new string('A', 1000);
        var longExpectation = new string('B', 1000);
        var longActual = new string('C', 1000);
        var longEndpoint = new string('D', 1000);
        var longVerb = new string('E', 1000);
        var longSnippet = new string('F', 1000);

        // Act
        var exception = new ApiAssertionException(
            longMessage,
            longExpectation,
            longActual,
            longEndpoint,
            longVerb,
            longSnippet);

        // Assert
        Assert.AreEqual(longMessage, exception.Message);
        Assert.AreEqual(longExpectation, exception.FailedExpectation);
        Assert.AreEqual(longActual, exception.ActualValues);
        Assert.AreEqual(longEndpoint, exception.Endpoint);
        Assert.AreEqual(longVerb, exception.HttpVerb);
        Assert.AreEqual(longSnippet, exception.ResponseBodySnippet);
    }
}
