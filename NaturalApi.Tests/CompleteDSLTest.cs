using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net.Http;
using System.Net;
using System.Text.Json;

namespace NaturalApi.Tests;

[TestClass]
public class CompleteDSLTest
{
    [TestMethod]
    public void FluentChain_Should_Handle_All_Constructs_Correctly()
    {
        // arrange
        var api = new Api(new MockHttpExecutorForDSL()); // facade, wraps IHttpExecutor stub

        var newUser = new
        {
            Name = "Dan",
            Email = "dan@test.local",
            Roles = new[] { "admin", "tester" }
        };

        // act + assert
        api
            .For("/users/{id}")
            .WithPathParam("id", 123)
            .WithHeaders(new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["X-Test-Mode"] = "true"
            })
            .WithQueryParams(new { includeInactive = false, region = "EU" })
            .UsingAuth("Bearer FAKE_TOKEN")
            .WithTimeout(TimeSpan.FromSeconds(10))
            .Post(newUser)
            .ShouldReturn<UserCreated>(
                status: 201,
                body => body.Id == 123 && body.Name == "Dan"
            )
            .Then(result =>
            {
                // verify access to low-level context
                Assert.IsNotNull(result.Response);
                Assert.AreEqual(201, result.StatusCode);
                Assert.IsTrue(result.Headers.ContainsKey("Content-Type"));
                Assert.IsTrue(result.Headers["Content-Type"].Contains("application/json"));

                // chain another call using the previous result
                result
                    .For($"/users/{result.BodyAs<UserCreated>().Id}")
                    .Get()
                .ShouldReturn<UserForDSL>(
                    status: 200,
                    body => body.Email == newUser.Email
                );
            });
    }
}

// Model classes for the test
public class UserCreated
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class UserForDSL
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
