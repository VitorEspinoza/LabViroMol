using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Research.Domain.Publications;

public record PublicationTranslationPersistentEvent() : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}