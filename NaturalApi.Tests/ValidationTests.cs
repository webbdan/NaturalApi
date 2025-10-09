using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net;
using System.Net.Http;

namespace NaturalApi.Tests;

[TestClass]
public class ValidationTests
{
    private Api _api = null!;

    [TestInitialize]
    public void Setup()
    {
        var httpClient = new HttpClient();
        var executor = new HttpClientExecutor(httpClient);
        _api = new Api(executor);
    }

    [TestMethod]
    public void Should_Validate_Status_Code_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(status: 200);
    }

    [TestMethod]
    public void Should_Validate_Body_Content_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(
            status: 200,
            body => body.id == 1 && body.userId == 1
        );
    }

    [TestMethod]
    public void Should_Validate_Headers_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(
            status: 200,
            headers: h => h.Keys.Any(k => k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
        );
    }

    [TestMethod]
    public void Should_Validate_All_Criteria_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(
            status: 200,
            body => body.id == 1 && !string.IsNullOrEmpty(body.title),
            headers: h => h.Keys.Any(k => k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
        );
    }

    [TestMethod]
    public void Should_Throw_ApiAssertionException_When_Status_Code_Mismatch()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.ThrowsException<ApiAssertionException>(() => 
            result.ShouldReturn<Post>(status: 404)
        );
    }

    [TestMethod]
    public void Should_Throw_ApiAssertionException_When_Body_Validation_Fails()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.ThrowsException<ApiAssertionException>(() => 
            result.ShouldReturn<Post>(
                status: 200,
                body => body.id == 999 // This should fail
            )
        );
    }

    [TestMethod]
    public void Should_Throw_ApiAssertionException_When_Header_Validation_Fails()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.ThrowsException<ApiAssertionException>(() => 
            result.ShouldReturn<Post>(
                status: 200,
                headers: h => h.ContainsKey("NonExistentHeader")
            )
        );
    }

    [TestMethod]
    public void Should_Validate_Array_Response_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithQueryParam("_limit", 3)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<List<Post>>(
            status: 200,
            posts => posts.Count >= 3 && posts.All(p => p.id > 0)
        );
    }

    [TestMethod]
    public void Should_Validate_Complex_Object_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/users/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<User>(
            status: 200,
            user => user.id == 1 && user.name == "Leanne Graham"
        );
    }

    [TestMethod]
    public void Should_Return_Self_For_Chaining_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        var chained = result.ShouldReturn<Post>(status: 200);
        Assert.AreSame(result, chained);
    }

    [TestMethod]
    public void Should_Validate_Without_Status_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(
            body => body.id == 1
        );
    }

    [TestMethod]
    public void Should_Validate_Without_Body_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(
            status: 200,
            headers: h => h.Keys.Any(k => k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
        );
    }

    [TestMethod]
    public void Should_Validate_Only_Headers_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        result.ShouldReturn<Post>(
            headers: h => h.Keys.Any(k => k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
        );
    }
}
