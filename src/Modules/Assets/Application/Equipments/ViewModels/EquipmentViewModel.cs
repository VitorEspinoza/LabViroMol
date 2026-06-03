using System;

namespace LabViroMol.Modules.Assets.Application.Equipments.ViewModels;

public record EquipmentViewModel(Guid Id, string Name, string Model, string Brand, string Code, string Description, string ImageUrl);