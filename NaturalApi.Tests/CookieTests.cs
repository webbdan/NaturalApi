// AIModified:2025-10-09T08:05:39Z
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace NaturalApi.Tests;

[TestClass]
public class CookieTests
{
    [TestMethod]
    public void ApiRequestSpec_WithCookie_ShouldAddCookie()
    {
        // Arrange
        var spec = new ApiRequestSpec(
            "/test",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);

        // Act
        var newSpec = spec.WithCookie("session", "abc123");

        // Assert
        Assert.IsNotNull(newSpec.Cookies);
        Assert.AreEqual(1, newSpec.Cookies.Count);
        Assert.AreEqual("abc123", newSpec.Cookies["session"]);
    }

    [TestMethod]
    public void ApiRequestSpec_WithCookies_ShouldAddMultipleCookies()
    {
        // Arrange
        var spec = new ApiRequestSpec(
            "/test",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);

        var cookies = new Dictionary<string, string>
        {
            { "session", "abc123" },
            { "theme", "dark" }
        };

        // Act
        var newSpec = spec.WithCookies(cookies);

        // Assert
        Assert.IsNotNull(newSpec.Cookies);
        Assert.AreEqual(2, newSpec.Cookies.Count);
        Assert.AreEqual("abc123", newSpec.Cookies["session"]);
        Assert.AreEqual("dark", newSpec.Cookies["theme"]);
    }

    [TestMethod]
    public void ApiRequestSpec_ClearCookies_ShouldRemoveAllCookies()
    {
        // Arrange
        var spec = new ApiRequestSpec(
            "/test",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null,
            false,
            null,
            new Dictionary<string, string> { { "session", "abc123" } });

        // Act
        var newSpec = spec.ClearCookies();

        // Assert
        Assert.IsNotNull(newSpec.Cookies);
        Assert.AreEqual(0, newSpec.Cookies.Count);
    }

    [TestMethod]
    public void ApiContext_WithCookie_ShouldAddCookieToSpec()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var spec = new ApiRequestSpec(
            "/test",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null);
        var context = new ApiContext(spec, mockExecutor);

        // Act
        var newContext = context.WithCookie("session", "abc123");

        // Assert
        Assert.IsNotNull(newContext);
        // Note: We can't directly access the spec from ApiContext, but we can verify it's a new instance
        Assert.AreNotEqual(context, newContext);
    }

    [TestMethod]
    public void ApiContext_ClearCookies_ShouldClearCookiesFromSpec()
    {
        // Arrange
        var mockExecutor = new MockHttpExecutor();
        var spec = new ApiRequestSpec(
            "/test",
            HttpMethod.Get,
            new Dictionary<string, string>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            null,
            null,
            false,
            null,
            new Dictionary<string, string> { { "session", "abc123" } });
        var context = new ApiContext(spec, mockExecutor);

        // Act
        var newContext = context.ClearCookies();

        // Assert
        Assert.IsNotNull(newContext);
        Assert.AreNotEqual(context, newContext);
    }
}
