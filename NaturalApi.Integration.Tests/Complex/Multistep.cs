using HandlebarsDotNet.Helpers;
using NaturalApi.Integration.Tests.Complex.Models;
using NaturalApi.Integration.Tests.Complex.Services;

namespace NaturalApi.Integration.Tests;

[TestClass]
public class Multistep
{
    [TestMethod]
    public void UsingFacadeAbstraction_ShouldSupportMultipleSteps()
    {
        var postsApi = new PostsService("https://jsonplaceholder.typicode.com");
    
        var posts = postsApi.List();
        var postcount = posts.Count;

        Assert.AreEqual(100, postcount);

        var post = postsApi.Create(new Post
        {
            title = "foo",
            body = "bar",
            userId = 1
        });

        Assert.AreEqual("foo", post.title);

        var singlePost = postsApi.Get(1);

        Assert.AreEqual(1, singlePost.id);


    }

    [TestMethod]
    public void PostShouldAddNewPost()
    {
        var api = new Api("https://jsonplaceholder.typicode.com");
    
        var posts = api.For("/posts")
            .Get()
            .ShouldReturn<List<Post>>();

        var postcount = posts.Count;

        Post postToAdd = new();
        postToAdd.title = "foo";
        postToAdd.body = "bar";
        postToAdd.userId = 1;


        var newPost = api.For("/posts")
            .Post(postToAdd)
            .ShouldReturn<Post>();

        posts = api.For("/posts")
            .Get()
            .ShouldReturn<List<Post>>();

    }
}


