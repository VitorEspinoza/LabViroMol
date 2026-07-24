using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class EmailMetrics
{
    private readonly Histogram<double> _latency;
    private readonly Counter<long> _failures;

    public EmailMetrics()
    {
        _latency = LabViroMolDiagnostics.Meter.CreateHistogram<double>(
            "email.latency",
            unit: "ms",
            description: "Latência de envio de e-mails.");

        _failures = LabViroMolDiagnostics.Meter.CreateCounter<long>(
            "email.failures",
            unit: "{failure}",
            description: "Falhas no envio de e-mails.");
    }

    public ValueStopwatch StartTimer() => ValueStopwatch.StartNew();

    public void RecordSuccess(ValueStopwatch timer) => _latency.Record(timer.GetElapsedTime().TotalMilliseconds);

    public void RecordFailure(ValueStopwatch timer)
    {
        _latency.Record(timer.GetElapsedTime().TotalMilliseconds);
        _failures.Add(1);
    }

    public readonly struct ValueStopwatch
    {
        private readonly long _startTimestamp;

        private ValueStopwatch(long startTimestamp) => _startTimestamp = startTimestamp;

        public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

        public TimeSpan GetElapsedTime()
        {
            var end = Stopwatch.GetTimestamp();
            var ticks = end - _startTimestamp;
            return TimeSpan.FromSeconds(ticks / (double)Stopwatch.Frequency);
        }
    }
}
