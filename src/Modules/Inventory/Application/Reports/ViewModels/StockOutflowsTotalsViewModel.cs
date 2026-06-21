namespace LabViroMol.Modules.Inventory.Application.Reports.ViewModels;

public record StockOutflowsTotalsViewModel(
    Guid MaterialId,
    string MaterialName,
    string MaterialType,
    string Unit,
    decimal ProjectConsumptionQuantity,
    decimal ExceptionOutQuantity,
    decimal TotalQuantity);
