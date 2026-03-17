using NaturalApi.Integration.Tests.Complex.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalApi.Integration.Tests.Complex.Services
{
    public class PostsFacade : ApiFacade
    {
        protected override string BaseRoute => "/posts";

        public PostsFacade(Api api) : base(api) { }

        public List<Post> List() => Get<List<Post>>();

        public Post Get(int id) => Get<Post>(id.ToString());

        public Post Create(Post model) => Post<Post, Post>(model);
    }

}
