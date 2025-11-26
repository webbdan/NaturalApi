using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi.Reporter;
using System.Collections.Generic;

namespace NaturalApi.Tests
{
    [TestClass]
    public class ReporterFactoryConfigTests
    {
        [TestMethod]
        public void ConfigBasedReporterSelection_ResolvesReporter()
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["NaturalApi:ReporterName"] = "null"
            };

            var cfg = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(cfg);

            // Add reporting and configure via NaturalApi config
            services.AddNaturalApiReporting();

            var natCfg = new NaturalApi.NaturalApiConfiguration { ReporterName = cfg["NaturalApi:ReporterName"] };
            services.AddNaturalApi(natCfg);

            var sp = services.BuildServiceProvider();

            // Resolve the registered interface and cast to concrete Api so we can inspect the Reporter
            var api = (Api)sp.GetRequiredService<IApi>();

            // Api should have chosen the NullReporter from config via factory
            Assert.IsInstanceOfType(api.Reporter, typeof(NullReporter));
        }
    }
}
