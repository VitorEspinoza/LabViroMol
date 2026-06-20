namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public sealed class OutboxOptions
{
    public int PollingIntervalSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 20;
}
