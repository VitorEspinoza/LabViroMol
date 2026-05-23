using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Kits;

public record KitItem(MaterialId MaterialId, Quantity Quantity);