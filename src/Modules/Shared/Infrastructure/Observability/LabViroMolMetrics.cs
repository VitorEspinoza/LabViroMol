using System.Diagnostics.Metrics;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class LabViroMolMetrics
{
    private const string SuccessOutcome = "success";
    private const string FailureOutcome = "failure";
    private const string NoErrorType = "none";

    private readonly Counter<long> _cqrsRequests;
    private readonly Histogram<double> _cqrsDuration;

    public LabViroMolMetrics()
    {
        _cqrsRequests = LabViroMolDiagnostics.Meter.CreateCounter<long>(
            "cqrs.requests",
            unit: "{request}",
            description: "Total de commands/queries executados via Mediator.");

        _cqrsDuration = LabViroMolDiagnostics.Meter.CreateHistogram<double>(
            "cqrs.duration",
            unit: "ms",
            description: "Duração de execução de um command/query.");
    }

    public void RecordSuccess(string requestName)
    {
        _cqrsRequests.Add(1,
            new KeyValuePair<string, object?>("request", requestName),
            new KeyValuePair<string, object?>("outcome", SuccessOutcome),
            new KeyValuePair<string, object?>("error_type", NoErrorType));
    }

    public void RecordFailure(string requestName, string errorType)
    {
        _cqrsRequests.Add(1,
            new KeyValuePair<string, object?>("request", requestName),
            new KeyValuePair<string, object?>("outcome", FailureOutcome),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    public void RecordDuration(string requestName, double durationMs)
    {
        _cqrsDuration.Record(durationMs,
            new KeyValuePair<string, object?>("request", requestName));
    }
}
