using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net;
using System.Net.Http;

namespace NaturalApi.Tests;

[TestClass]
public class BodyDeserializationTests
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
    public void Should_Deserialize_Single_Object_When_Using_BodyAs()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        
        var post = result.BodyAs<Post>();
        Assert.IsNotNull(post);
        Assert.AreEqual(1, post.id);
        Assert.AreEqual("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", post.title);
    }

    [TestMethod]
    public void Should_Deserialize_Array_When_Using_BodyAs()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithQueryParam("_limit", 3)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        
        var posts = result.BodyAs<List<Post>>();
        Assert.IsNotNull(posts);
        Assert.IsTrue(posts.Count >= 3);
        Assert.AreEqual(1, posts[0].id);
        Assert.AreEqual(2, posts[1].id);
        Assert.AreEqual(3, posts[2].id);
    }

    [TestMethod]
    public void Should_Deserialize_Complex_Object_When_Using_BodyAs()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/users/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        
        var user = result.BodyAs<User>();
        Assert.IsNotNull(user);
        Assert.AreEqual(1, user.id);
        Assert.AreEqual("Leanne Graham", user.name);
        Assert.IsNotNull(user.address);
        Assert.AreEqual("Gwenborough", user.address.city);
    }

    [TestMethod]
    public void Should_Throw_InvalidOperationException_When_Body_Is_Empty()
    {
        // Arrange - Create a mock response with empty body
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };
        var result = new ApiResultContext(response, new MockHttpExecutor());

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => result.BodyAs<Post>());
    }

    [TestMethod]
    public void Should_Throw_InvalidOperationException_When_JSON_Is_Invalid()
    {
        // Arrange - Create a mock response with invalid JSON
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json")
        };
        var result = new ApiResultContext(response, new MockHttpExecutor());

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => result.BodyAs<Post>());
    }

    [TestMethod]
    public void Should_Deserialize_Anonymous_Type_When_Using_BodyAs()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        
        var post = result.BodyAs<dynamic>();
        Assert.IsNotNull(post);
    }

    [TestMethod]
    public void Should_Deserialize_Nested_Objects_When_Using_BodyAs()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1/comments")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        
        var comments = result.BodyAs<List<Comment>>();
        Assert.IsNotNull(comments);
        Assert.IsTrue(comments.Count > 0);
        Assert.IsNotNull(comments[0].Email);
    }
}

// Test data models
public class Post
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public int userId { get; set; }
}

public class User
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public Address address { get; set; } = new();
}

public class Address
{
    public string street { get; set; } = string.Empty;
    public string suite { get; set; } = string.Empty;
    public string city { get; set; } = string.Empty;
    public string zipcode { get; set; } = string.Empty;
}

public class Comment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int PostId { get; set; }
}
