using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public record PersistentTranslationEvent : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}