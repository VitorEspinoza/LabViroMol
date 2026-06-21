using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Research.Contracts;

public sealed record PositionTranslationEvent(
    Guid PositionId
) : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}
