namespace LabViroMol.Modules.Assets.Application.Equipments.ViewModels;

public record EquipmentAdminSummaryViewModel(
    Guid Id,
    string Code,
    string Name,
    string Model,
    string Brand,
    string? Location,
    string Status);
