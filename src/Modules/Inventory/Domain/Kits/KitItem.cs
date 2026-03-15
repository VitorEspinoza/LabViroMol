using LabViroMol.Modules.Inventory.Domain.Materials;

namespace LabViroMol.Modules.Inventory.Domain.Kits;

public record KitItem(MaterialId MaterialId, Quantity Quantity);