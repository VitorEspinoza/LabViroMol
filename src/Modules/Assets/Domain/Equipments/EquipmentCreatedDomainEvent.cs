using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Assets.Domain.Equipments;

public record EquipmentCreatedDomainEvent(Equipment Equipment) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}