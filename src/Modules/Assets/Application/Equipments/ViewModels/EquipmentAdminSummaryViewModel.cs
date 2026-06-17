namespace LabViroMol.Modules.Assets.Application.Equipments.ViewModels;

public record EquipmentAdminSummaryViewModel(
    Guid EquipmentId,
    string Code,
    string Name,
    string Model,
    string Brand,
    string? Location,
    string? Description,
    string Status,
    string? ImageUrl);
