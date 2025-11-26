using NaturalApi;
using NaturalApi.Reporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NaturalApi.Tests;

[TestClass]
public class ReporterTests
{
    [TestMethod]
    public void DefaultReporter_IsUsed_WhenNoOverrides()
    {
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);

        var result = api.For("/test").Get();

        // DefaultReporter writes to console; ensure execution succeeded
        Assert.AreEqual(200, result.StatusCode);
    }

    class TestCollectorReporter : INaturalReporter
    {
        public bool RequestSent { get; private set; }
        public bool ResponseReceived { get; private set; }
        public void OnRequestSent(ApiRequestSpec request) => RequestSent = true;
        public void OnResponseReceived(IApiResultContext response) => ResponseReceived = true;
        public void OnAssertionPassed(string message, ApiResultContext response) { }
        public void OnAssertionFailed(string message, ApiResultContext response) { }
    }

    [TestMethod]
    public void PerCallReporter_Overrides_ExecutorReporter()
    {
        var executor = new MockTimeoutHttpExecutor();
        var api = new Api(executor);

        var collector = new TestCollectorReporter();

        var result = api.For("/test")
            .WithReporter(collector)
            .Get();

        Assert.IsTrue(collector.RequestSent);
        Assert.IsTrue(collector.ResponseReceived);
    }

    [TestMethod]
    public void DefaultsProviderReporter_IsUsed_WhenProvided()
    {
        var executor = new MockTimeoutHttpExecutor();

        var defaults = new TestDefaults();
        var api = new Api(executor, defaults);

        var result = api.For("/test").Get();

        Assert.IsTrue(defaults.ReporterUsed);
        Assert.AreEqual(200, result.StatusCode);
    }

    class TestDefaults : IApiDefaultsProvider
    {
        public Uri? BaseUri => null;
        public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public IApiAuthProvider? AuthProvider => null;

        public bool ReporterUsed { get; private set; }

        public INaturalReporter? Reporter => new CollectingReporter(this);

        private class CollectingReporter : INaturalReporter
        {
            private readonly TestDefaults _outer;
            public CollectingReporter(TestDefaults outer) { _outer = outer; }

            public void OnRequestSent(ApiRequestSpec request)
            {
                _outer.ReporterUsed = true;
            }
            public void OnResponseReceived(IApiResultContext response) { }
            public void OnAssertionPassed(string message, ApiResultContext response) { }
            public void OnAssertionFailed(string message, ApiResultContext response) { }
        }
    }
}
