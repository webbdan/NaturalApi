using NaturalApi.Reporter;

namespace NaturalApi.Reporter
{
    public interface IReporterFactory
    {
        INaturalReporter Get(string name);
    }
}
