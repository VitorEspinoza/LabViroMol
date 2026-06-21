namespace LabViroMol.Modules.Inventory.Application.Reports.ViewModels;

public record StockOutflowsByProjectViewModel(
    Guid ProjectId,
    string ProjectTitle,
    Guid MaterialId,
    string MaterialName,
    string MaterialType,
    string Unit,
    decimal Quantity);
