# Reporting

NaturalApi supports pluggable reporting to allow logging of requests, responses and assertion results.

Priority order for reporter selection:
1. Per-call reporter set via `WithReporter()` on `IApiContext`.
2. Configuration-level reporter provided by `IApiDefaultsProvider.Reporter`.
3. Reporter configured on the `IApi` instance or executor (constructor arg / `api.Reporter`).
4. Fallback to `DefaultReporter` (console output).

Usage examples

Per-call reporter:

```csharp
var result = api.For("/users")
    .WithReporter(new CompactConsoleReporter())
    .Get();
```

Configuration-level reporter:

```csharp
public class MyDefaults : IApiDefaultsProvider
{
    public Uri? BaseUri => new Uri("https://api.example.com");
    public IDictionary<string, string> DefaultHeaders => new Dictionary<string, string>();
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public IApiAuthProvider? AuthProvider => null;
    public NaturalApi.Reporter.INaturalReporter? Reporter => new CompactConsoleReporter();
}

var api = new Api(new HttpClientExecutor(new HttpClient()), new MyDefaults());
```

Set reporter per Api instance:

```csharp
var api = new Api(new HttpClientExecutor(new HttpClient()));
api.Reporter = new CompactConsoleReporter();
```

Configuring reporters via appsettings + DI

You cannot serialize an `INaturalReporter` instance in JSON. Instead store a reporter key/name in configuration and map that key to a concrete reporter via DI/factory. The library provides a small `IReporterFactory` and `AddNaturalApiReporting()` helper to register known reporters and a default factory. Use `NaturalApiConfiguration.ReporterName` to pass the chosen name into `AddNaturalApi(...)`.

Example `appsettings.json`:

```json
{
  "NaturalApi": {
    "ReporterName": "compact" // or "default", "null", etc.
  }
}
```

Minimal DI wiring (reads reporter name from IConfiguration and registers NaturalApi):

```csharp
// in Program.cs or test startup
services.AddSingleton<IConfiguration>(configuration);

// register reporting support (default reporters + factory)
services.AddNaturalApiReporting();

// create NaturalApiConfiguration from config
var natCfg = new NaturalApiConfiguration
{
    BaseUrl = configuration["ApiSettings:BaseUrl"],
    ReporterName = configuration["NaturalApi:ReporterName"]
};

services.AddNaturalApi(natCfg);
```

Examples derived from tests

1) Test-level per-call override (from `ReporterTests.PerCallReporter_Overrides_ExecutorReporter`):

```csharp
var executor = new MockTimeoutHttpExecutor();
var api = new Api(executor);

var collector = new TestCollectorReporter();

api.For("/test")
   .WithReporter(collector)
   .Get();

// collector.RequestSent and collector.ResponseReceived are asserted in test
```

2) Defaults-provider reporter (from `ReporterTests.DefaultsProviderReporter_IsUsed_WhenProvided`):

```csharp
var executor = new MockTimeoutHttpExecutor();
var defaults = new TestDefaults(); // TestDefaults.Reporter returns a reporter that flips a flag
var api = new Api(executor, defaults);

api.For("/test").Get();

// defaults.ReporterUsed is asserted in test
```

3) Configuration-driven selection (from `ReporterFactoryConfigTests` and `ReportingConfigIntegrationTests`):

```csharp
// appsettings.json contains: { "NaturalApi": { "ReporterName": "compact" } }
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

services.AddNaturalApiReporting(); // registers compact mapping

var natCfg = new NaturalApiConfiguration { ReporterName = configuration["NaturalApi:ReporterName"] };
services.AddNaturalApi(natCfg);

var sp = services.BuildServiceProvider();
var api = (Api)sp.GetRequiredService<IApi>();

// api.Reporter should be CompactReporter
```

4) Custom mapping (from `ReporterMappingTests`):

```csharp
var services = new ServiceCollection();
services.AddSingleton<CustomTestReporter>();
var map = new Dictionary<string, Type> { ["custom"] = typeof(CustomTestReporter) };
services.AddNaturalApiReporting(map);
var sp = services.BuildServiceProvider();
var factory = sp.GetRequiredService<IReporterFactory>();
var reporter = factory.Get("custom");
// reporter is CustomTestReporter
```

Notes
- Reporter instances should be thread-safe (prefer stateless singletons) because they may be invoked concurrently.
- Appsettings stores only the reporter key; all construction/lifetime is handled by DI/factories in code.
- If you need runtime or per-environment reporter selection, bind different `NaturalApi:ReporterName` values in environment-specific appsettings files or environment variables.
