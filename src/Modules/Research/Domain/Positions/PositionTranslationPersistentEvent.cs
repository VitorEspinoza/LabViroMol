using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Research.Domain.Positions;

public record PositionTranslationPersistentEvent() : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}