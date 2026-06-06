using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Inventory.Domain.Materials;

public record LowStockDomainEvent(MaterialId MaterialId, string MaterialName, decimal CurrentQuantity) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
