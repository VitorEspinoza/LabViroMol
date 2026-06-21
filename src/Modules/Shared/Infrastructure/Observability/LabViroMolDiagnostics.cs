using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public static class LabViroMolDiagnostics
{
    public const string Name = "LabViroMol";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);
}
