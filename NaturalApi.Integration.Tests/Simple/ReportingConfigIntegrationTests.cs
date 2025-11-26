using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi.Reporter;
using System.IO;

namespace NaturalApi.Integration.Tests.Simple
{
    [TestClass]
    public class ReportingConfigIntegrationTests
    {
        [TestMethod]
        public void AddNaturalApi_From_AppSettings_Uses_CompactReporter()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Simple/appsettings.json", optional: false)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // Register reporting (default reporters including compact)
            services.AddNaturalApiReporting();

            // Bind NaturalApi configuration
            var natCfg = new NaturalApi.NaturalApiConfiguration
            {
                BaseUrl = configuration["ApiSettings:BaseUrl"],
                ReporterName = configuration["NaturalApi:ReporterName"]
            };

            services.AddNaturalApi(natCfg);

            var sp = services.BuildServiceProvider();
            var api = sp.GetRequiredService<IApi>() as Api;

            Assert.IsNotNull(api);
            Assert.IsInstanceOfType(api.Reporter, typeof(CompactReporter));
        }
    }
}
