using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace NaturalApi.Reporter
{
    public class ReporterFactory : IReporterFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IDictionary<string, Type> _map;

        public ReporterFactory(IServiceProvider provider, IDictionary<string, Type>? map = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _map = map ?? new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = typeof(DefaultReporter),
                ["null"] = typeof(NullReporter),
                ["compact"] = typeof(CompactReporter)
            };
        }

        public INaturalReporter Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return _provider.GetService<INaturalReporter>() ?? new DefaultReporter();

            if (!_map.TryGetValue(name, out var t))
                throw new KeyNotFoundException($"Reporter '{name}' is not registered in ReporterFactory");

            var instance = _provider.GetService(t) ?? Activator.CreateInstance(t);
            if (instance is INaturalReporter reporter) return reporter;

            throw new InvalidOperationException($"Registered reporter type {t.FullName} does not implement INaturalReporter");
        }
    }
}
