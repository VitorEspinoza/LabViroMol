using System;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Application.Kits.Commands.Shared;

public record KitItemInputModel(Guid Id, decimal Quantity)
{
    public KitItem ToValueObject()
    {
        return new KitItem(MaterialId.From(Id), (Quantity)Quantity);
    }
}
