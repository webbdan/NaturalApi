using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;

namespace NaturalApi.Tests;

[TestClass]
public class FluentSyntaxTests
{
    private IApi _api;

    [TestInitialize]
    public void Setup()
    {
        _api = new Api(new HttpClientExecutor(new HttpClient()));
    }

    [TestMethod]
    public void Should_Get_Single_Post()
    {
        // Act & Assert - Getting a single resource
        PostDTO response = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get()
            .ShouldReturn<PostDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.id);
        Assert.IsNotNull(response.title);
        Assert.IsNotNull(response.body);
        Assert.AreEqual(1, response.userId);
        
        Console.WriteLine($"✅ GET /posts/1 - ID: {response.id}, Title: {response.title}");
    }

    [TestMethod]
    public void Should_Get_All_Posts()
    {
        // Act & Assert - Listing all resources
        List<PostDTO> response = _api.For("https://jsonplaceholder.typicode.com/posts")
            .Get()
            .ShouldReturn<List<PostDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count > 0);
        Assert.AreEqual(1, response.First().id);
        
        Console.WriteLine($"✅ GET /posts - Count: {response.Count}, First ID: {response.First().id}");
    }

    [TestMethod]
    public void Should_Create_New_Post()
    {
        // Arrange
        var newPost = new
        {
            title = "foo",
            body = "bar",
            userId = 1
        };

        // Act & Assert - Creating a resource
        PostDTO response = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json; charset=UTF-8"
            })
            .Post(newPost)
            .ShouldReturn<PostDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(101, response.id); // JSONPlaceholder returns 101 for new posts
        Assert.AreEqual("foo", response.title);
        Assert.AreEqual("bar", response.body);
        Assert.AreEqual(1, response.userId);
        
        Console.WriteLine($"✅ POST /posts - Created ID: {response.id}, Title: {response.title}");
    }

    [TestMethod]
    public void Should_Update_Post_With_PUT()
    {
        // Arrange
        var updatedPost = new
        {
            id = 1,
            title = "foo",
            body = "bar",
            userId = 1
        };

        // Act & Assert - Updating a resource
        PostDTO response = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json; charset=UTF-8"
            })
            .Put(updatedPost)
            .ShouldReturn<PostDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.id);
        Assert.AreEqual("foo", response.title);
        Assert.AreEqual("bar", response.body);
        Assert.AreEqual(1, response.userId);
        
        Console.WriteLine($"✅ PUT /posts/1 - Updated ID: {response.id}, Title: {response.title}");
    }

    [TestMethod]
    public void Should_Patch_Post_With_PATCH()
    {
        // Arrange
        var patchData = new
        {
            title = "foo"
        };

        // Act & Assert - Patching a resource
        PostDTO response = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json; charset=UTF-8"
            })
            .Patch(patchData)
            .ShouldReturn<PostDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.id);
        Assert.AreEqual("foo", response.title);
        Assert.AreEqual(1, response.userId);
        
        Console.WriteLine($"✅ PATCH /posts/1 - Patched ID: {response.id}, Title: {response.title}");
    }

    [TestMethod]
    public void Should_Delete_Post()
    {
        // Act & Assert - Deleting a resource
        _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Delete()
            .ShouldReturn(200); // JSONPlaceholder returns 200 for DELETE
        
        Console.WriteLine($"✅ DELETE /posts/1 - Successfully deleted");
    }

    [TestMethod]
    public void Should_Filter_Posts_By_User()
    {
        // Act & Assert - Filtering resources with query parameters
        List<PostDTO> response = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithQueryParam("userId", 1)
            .Get()
            .ShouldReturn<List<PostDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count > 0);
        Assert.IsTrue(response.All(p => p.userId == 1));
        
        Console.WriteLine($"✅ GET /posts?userId=1 - Count: {response.Count}, All from user 1: {response.All(p => p.userId == 1)}");
    }

    [TestMethod]
    public void Should_Get_Nested_Comments_For_Post()
    {
        // Act & Assert - Listing nested resources
        List<CommentDTO> response = _api.For("https://jsonplaceholder.typicode.com/posts/1/comments")
            .Get()
            .ShouldReturn<List<CommentDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count > 0);
        Assert.IsTrue(response.All(c => c.postId == 1));
        
        Console.WriteLine($"✅ GET /posts/1/comments - Count: {response.Count}, All for post 1: {response.All(c => c.postId == 1)}");
    }

    [TestMethod]
    public void Should_Get_User_With_Authentication()
    {
        // Act & Assert - Using authentication
        UserDTO response = _api.For("https://jsonplaceholder.typicode.com/users/1")
            .UsingAuth("Bearer fake-token")
            .Get()
            .ShouldReturn<UserDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.id);
        Assert.IsNotNull(response.name);
        Assert.IsNotNull(response.email);
        
        Console.WriteLine($"✅ GET /users/1 with auth - ID: {response.id}, Name: {response.name}");
    }

    [TestMethod]
    public void Should_Chain_Multiple_Operations()
    {
        // Act & Assert - Chaining operations
        var result = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .Get()
            .ShouldReturn<PostDTO>();

        // Verify first operation
        Assert.AreEqual(1, result.id);
        Console.WriteLine($"✅ First operation - Post ID: {result.id}");

        // Chain to get comments for this post
        List<CommentDTO> comments = _api.For($"https://jsonplaceholder.typicode.com/posts/{result.id}/comments")
            .Get()
            .ShouldReturn<List<CommentDTO>>();

        // Verify chained operation
        Assert.IsNotNull(comments);
        Assert.IsTrue(comments.Count > 0);
        Console.WriteLine($"✅ Chained operation - Comments count: {comments.Count}");
    }

    [TestMethod]
    public void Should_Get_User_Albums()
    {
        // Act & Assert - Getting user albums (nested resource)
        List<AlbumDTO> response = _api.For("https://jsonplaceholder.typicode.com/users/1/albums")
            .Get()
            .ShouldReturn<List<AlbumDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count > 0);
        Assert.IsTrue(response.All(a => a.userId == 1));
        
        Console.WriteLine($"✅ GET /users/1/albums - Count: {response.Count}, All for user 1: {response.All(a => a.userId == 1)}");
    }

    [TestMethod]
    public void Should_Get_User_Todos()
    {
        // Act & Assert - Getting user todos (nested resource)
        List<TodoDTO> response = _api.For("https://jsonplaceholder.typicode.com/users/1/todos")
            .Get()
            .ShouldReturn<List<TodoDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count > 0);
        Assert.IsTrue(response.All(t => t.userId == 1));
        
        Console.WriteLine($"✅ GET /users/1/todos - Count: {response.Count}, All for user 1: {response.All(t => t.userId == 1)}");
    }

    [TestMethod]
    public void Should_Get_Album_Photos()
    {
        // Act & Assert - Getting album photos (nested resource)
        List<PhotoDTO> response = _api.For("https://jsonplaceholder.typicode.com/albums/1/photos")
            .Get()
            .ShouldReturn<List<PhotoDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count > 0);
        Assert.IsTrue(response.All(p => p.albumId == 1));
        
        Console.WriteLine($"✅ GET /albums/1/photos - Count: {response.Count}, All for album 1: {response.All(p => p.albumId == 1)}");
    }

    [TestMethod]
    public void Should_Use_Complex_Query_Parameters()
    {
        // Act & Assert - Using multiple query parameters
        List<PostDTO> response = _api.For("https://jsonplaceholder.typicode.com/posts")
            .WithQueryParams(new Dictionary<string, object>
            {
                ["userId"] = 1,
                ["_limit"] = 5
            })
            .Get()
            .ShouldReturn<List<PostDTO>>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Count <= 5);
        Assert.IsTrue(response.All(p => p.userId == 1));
        
        Console.WriteLine($"✅ GET /posts?userId=1&_limit=5 - Count: {response.Count}, All from user 1: {response.All(p => p.userId == 1)}");
    }

    [TestMethod]
    public void Should_Use_Path_Parameters()
    {
        // Act & Assert - Using path parameters
        PostDTO response = _api.For("https://jsonplaceholder.typicode.com/posts/{id}")
            .WithPathParam("id", 1)
            .Get()
            .ShouldReturn<PostDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.id);
        
        Console.WriteLine($"✅ GET /posts/{1} with path param - ID: {response.id}");
    }

    [TestMethod]
    public void Should_Use_Custom_Headers()
    {
        // Act & Assert - Using custom headers
        PostDTO response = _api.For("https://jsonplaceholder.typicode.com/posts/1")
            .WithHeaders(new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["User-Agent"] = "NaturalApi-Test/1.0",
                ["X-Test-Mode"] = "true"
            })
            .Get()
            .ShouldReturn<PostDTO>();

        // Verify the response
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.id);
        
        Console.WriteLine($"✅ GET /posts/1 with custom headers - ID: {response.id}");
    }
}

// DTOs for testing based on JSONPlaceholder API
public class PostDTO
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public int userId { get; set; }
}

public class CommentDTO
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public int postId { get; set; }
}

public class UserDTO
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public AddressDTO address { get; set; } = new();
    public string phone { get; set; } = string.Empty;
    public string website { get; set; } = string.Empty;
    public CompanyDTO company { get; set; } = new();
}

public class AddressDTO
{
    public string street { get; set; } = string.Empty;
    public string suite { get; set; } = string.Empty;
    public string city { get; set; } = string.Empty;
    public string zipcode { get; set; } = string.Empty;
    public GeoDTO geo { get; set; } = new();
}

public class GeoDTO
{
    public string lat { get; set; } = string.Empty;
    public string lng { get; set; } = string.Empty;
}

public class CompanyDTO
{
    public string name { get; set; } = string.Empty;
    public string catchPhrase { get; set; } = string.Empty;
    public string bs { get; set; } = string.Empty;
}

public class AlbumDTO
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public int userId { get; set; }
}

public class TodoDTO
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public bool completed { get; set; }
    public int userId { get; set; }
}

public class PhotoDTO
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string thumbnailUrl { get; set; } = string.Empty;
    public int albumId { get; set; }
}