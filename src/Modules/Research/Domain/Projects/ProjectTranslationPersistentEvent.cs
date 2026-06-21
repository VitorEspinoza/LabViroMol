using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Research.Domain.Projects;

public record ProjectTranslationPersistentEvent() : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}