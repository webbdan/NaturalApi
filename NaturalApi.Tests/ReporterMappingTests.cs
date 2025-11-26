using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi.Reporter;
using System;
using System.Collections.Generic;

namespace NaturalApi.Tests
{
    [TestClass]
    public class ReporterMappingTests
    {
        [TestMethod]
        public void CustomMapping_Used_By_ReporterFactory()
        {
            var services = new ServiceCollection();

            // register custom reporter type
            services.AddSingleton<CustomTestReporter>();

            var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["custom"] = typeof(CustomTestReporter)
            };

            services.AddNaturalApiReporting(map);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IReporterFactory>();

            var reporter = factory.Get("custom");

            Assert.IsInstanceOfType(reporter, typeof(CustomTestReporter));
        }

        private class CustomTestReporter : INaturalReporter
        {
            public void OnAssertionFailed(string message, ApiResultContext response) { }
            public void OnAssertionPassed(string message, ApiResultContext response) { }
            public void OnRequestSent(ApiRequestSpec request) { }
            public void OnResponseReceived(IApiResultContext response) { }
        }
    }
}
