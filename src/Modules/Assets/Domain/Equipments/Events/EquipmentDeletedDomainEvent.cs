using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Assets.Domain.Equipments.Events;

public record EquipmentDeletedDomainEvent(EquipmentId EquipmentId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
