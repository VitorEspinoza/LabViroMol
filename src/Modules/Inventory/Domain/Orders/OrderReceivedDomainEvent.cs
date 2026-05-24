using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Orders;

public record OrderReceivedDomainEvent(OrderId OrderId, MaterialId MaterialId, Quantity QuantityReceived, UserId ReceivedBy) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}

