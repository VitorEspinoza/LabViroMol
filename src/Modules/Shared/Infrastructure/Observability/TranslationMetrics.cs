using System.Diagnostics.Metrics;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class TranslationMetrics
{
    private readonly Counter<long> _records;
    private readonly Counter<long> _failures;
    private readonly Histogram<double> _duration;

    public TranslationMetrics()
    {
        _records = LabViroMolDiagnostics.Meter.CreateCounter<long>(
            "translation.records",
            unit: "{record}",
            description: "Registros enviados para tradução (LibreTranslate).");

        _failures = LabViroMolDiagnostics.Meter.CreateCounter<long>(
            "translation.failures",
            unit: "{failure}",
            description: "Falhas de tradução.");

        _duration = LabViroMolDiagnostics.Meter.CreateHistogram<double>(
            "translation.duration",
            unit: "ms",
            description: "Duração de uma chamada de tradução.");
    }

    public void RecordRecords(string job, long count)
    {
        _records.Add(count,
            new KeyValuePair<string, object?>("job", job));
    }

    public void RecordFailure(string job)
    {
        _failures.Add(1,
            new KeyValuePair<string, object?>("job", job));
    }

    public void RecordDuration(string job, double durationMs)
    {
        _duration.Record(durationMs,
            new KeyValuePair<string, object?>("job", job));
    }
}
