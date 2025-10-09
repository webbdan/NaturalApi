using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class IntegrationTests
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
    public void Should_Get_Post_By_Id_When_Using_GET_With_Path_Parameter()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/{id}")
            .WithPathParam("id", 1)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.Contains("\"id\": 1"));
    }

    [TestMethod]
    public void Should_Get_All_Posts_When_Using_GET_Without_Parameters()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.StartsWith("["));
        Assert.IsTrue(result.RawBody.EndsWith("]"));
    }

    [TestMethod]
    public void Should_Get_Posts_By_User_When_Using_GET_With_Query_Parameter()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithQueryParam("userId", 1)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.StartsWith("["));
    }

    [TestMethod]
    public void Should_Get_Posts_With_Multiple_Query_Parameters_When_Using_GET()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithQueryParams(new { userId = 1, _limit = 5 })
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
    }

    [TestMethod]
    public void Should_Create_Post_When_Using_POST_With_Body()
    {
        // Arrange
        var newPost = new
        {
            title = "Test Post",
            body = "This is a test post",
            userId = 1
        };

        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithHeader("Content-Type", "application/json")
            .Post(newPost);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(201, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.Contains("Test Post"));
    }

    [TestMethod]
    public void Should_Update_Post_When_Using_PUT_With_Body()
    {
        // Arrange
        var updatedPost = new
        {
            id = 1,
            title = "Updated Post",
            body = "This post has been updated",
            userId = 1
        };

        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/{id}")
            .WithPathParam("id", 1)
            .WithHeader("Content-Type", "application/json")
            .Put(updatedPost);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.Contains("Updated Post"));
    }

    [TestMethod]
    public void Should_Patch_Post_When_Using_PATCH_With_Body()
    {
        // Arrange
        var patchData = new
        {
            title = "Patched Post"
        };

        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/{id}")
            .WithPathParam("id", 1)
            .WithHeader("Content-Type", "application/json")
            .Patch(patchData);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.Contains("Patched Post"));
    }

    [TestMethod]
    public void Should_Delete_Post_When_Using_DELETE()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/{id}")
            .WithPathParam("id", 1)
            .Delete();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public void Should_Get_Comments_For_Post_When_Using_Nested_Route()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/{id}/comments")
            .WithPathParam("id", 1)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
        Assert.IsTrue(result.RawBody.StartsWith("["));
    }

    [TestMethod]
    public void Should_Use_Custom_Headers_When_Provided()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .WithHeader("Accept", "application/json")
            .WithHeader("User-Agent", "NaturalApi-Test")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
    }

    [TestMethod]
    public void Should_Use_Multiple_Headers_When_Provided()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["User-Agent"] = "NaturalApi-Test",
            ["X-Custom-Header"] = "test-value"
        };

        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .WithHeaders(headers)
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.RawBody);
    }

    [TestMethod]
    public void Should_Handle_404_Error_When_Requesting_Non_Existent_Resource()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/99999")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(404, result.StatusCode);
    }

    [TestMethod]
    public void Should_Deserialize_Response_Body_When_Using_BodyAs()
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
    public void Should_Validate_Response_When_Using_ShouldReturn()
    {
        // Act
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        
        result.ShouldReturn<Post>(status: 200, body => body.id == 1);
    }
}
