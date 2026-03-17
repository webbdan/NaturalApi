using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalApi.Integration.Tests.Complex.Services
{
    public abstract class ApiFacade
    {
        protected readonly Api Api;
        protected abstract string BaseRoute { get; }

        protected ApiFacade(Api api)
        {
            Api = api;
        }

        protected T Get<T>(string route = "")
            => Api.For(Combine(route)).Get().ShouldReturn<T>();

        protected T Post<TBody, T>(TBody body, string route = "")
            => Api.For(Combine(route)).Post(body).ShouldReturn<T>();

        private string Combine(string route)
            => string.IsNullOrWhiteSpace(route)
                ? BaseRoute
                : $"{BaseRoute}/{route}";
    }

}
