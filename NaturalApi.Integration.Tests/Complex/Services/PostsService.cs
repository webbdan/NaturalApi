using NaturalApi.Integration.Tests.Complex.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalApi.Integration.Tests.Complex.Services
{
    public class PostsService
    {
        private readonly Api _api;
        private readonly string baseRoute = "/posts";

        public PostsService(Api api)
        {
            _api = api;
        }

        public PostsService(String baseUrl)
        {
            _api = new Api(baseUrl);
        }

        public List<Post> List()
            => _api.For($"{baseRoute}").Get().ShouldReturn<List<Post>>();

        public Post Get(int id)
            => _api.For($"{baseRoute}/{id.ToString()}").Get().ShouldReturn<Post>();

        public Post Create(Post model)
            => _api.For($"{baseRoute}").Post(model).ShouldReturn<Post>();

    }

}
