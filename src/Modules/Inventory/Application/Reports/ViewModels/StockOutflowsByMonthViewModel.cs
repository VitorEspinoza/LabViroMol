namespace LabViroMol.Modules.Inventory.Application.Reports.ViewModels;

public record StockOutflowsByMonthViewModel(
    int Year,
    int Month,
    Guid MaterialId,
    string MaterialName,
    string MaterialType,
    string Unit,
    string OutflowType,
    decimal Quantity);
