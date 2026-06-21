using System.Diagnostics.Metrics;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class OutboxMetrics
{
    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesFailed;
    private readonly Histogram<double> _batchDuration;
    private readonly Gauge<long> _pending;

    public OutboxMetrics()
    {
        _messagesProcessed = LabViroMolDiagnostics.Meter.CreateCounter<long>(
            "outbox.messages.processed",
            unit: "{message}",
            description: "Mensagens de outbox processadas com sucesso.");

        _messagesFailed = LabViroMolDiagnostics.Meter.CreateCounter<long>(
            "outbox.messages.failed",
            unit: "{message}",
            description: "Mensagens de outbox que falharam ao processar.");

        _batchDuration = LabViroMolDiagnostics.Meter.CreateHistogram<double>(
            "outbox.batch.duration",
            unit: "ms",
            description: "Duração de um ciclo de processamento de batch do outbox.");

        _pending = LabViroMolDiagnostics.Meter.CreateGauge<long>(
            "outbox.pending",
            unit: "{message}",
            description: "Quantidade de mensagens pendentes no outbox no momento da medição.");
    }

    public void RecordProcessed(string eventType) =>
        _messagesProcessed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

    public void RecordFailed(string eventType) =>
        _messagesFailed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

    public void RecordBatchDuration(double durationMs) =>
        _batchDuration.Record(durationMs);

    public void RecordPending(long count) =>
        _pending.Record(count);
}
