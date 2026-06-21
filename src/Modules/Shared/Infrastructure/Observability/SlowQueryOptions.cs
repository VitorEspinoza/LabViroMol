namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class SlowQueryOptions
{
    public int SlowQueryMs { get; set; } = 500;

    internal int ThresholdMs => SlowQueryMs;
}
