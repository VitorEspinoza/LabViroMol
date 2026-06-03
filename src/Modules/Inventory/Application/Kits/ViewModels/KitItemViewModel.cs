using System;

namespace LabViroMol.Modules.Inventory.Application.Kits.ViewModels;

public record KitItemViewModel(Guid MaterialId, string Name, decimal Quantity, string Unit);
