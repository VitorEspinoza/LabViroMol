using System;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Materials;

public record struct StockTransactionId(Guid Value) :  IStrongId<StockTransactionId>
{
    public static StockTransactionId New() => new(Guid.CreateVersion7());
    
    public static StockTransactionId From(Guid value) => new(value);

    public static implicit operator Guid(StockTransactionId id) => id.Value;
};
