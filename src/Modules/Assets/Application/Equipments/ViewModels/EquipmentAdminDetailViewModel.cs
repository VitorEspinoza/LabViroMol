namespace LabViroMol.Modules.Assets.Application.Equipments.ViewModels;

public record EquipmentAdminDetailViewModel(
    Guid EquipmentId,
    string Name,
    string Model,
    string Brand,
    string Code,
    string? Location,
    string Description,
    string? ImageUrl);
