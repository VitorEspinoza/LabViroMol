using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Assets.Domain.Equipments.Events;

public record EquipmentTranslationPersistentEvent() : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}