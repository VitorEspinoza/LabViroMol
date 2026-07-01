using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Orders;

public record struct OrderId(Guid Value) : IStrongId<OrderId>
{
    public static OrderId New() => new(Guid.CreateVersion7());

    public static OrderId From(Guid value) => new(value);

    public static implicit operator Guid(OrderId id) => id.Value;
};

