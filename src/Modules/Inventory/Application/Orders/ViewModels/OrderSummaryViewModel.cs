namespace LabViroMol.Modules.Inventory.Application.Orders.ViewModels;

public record OrderSummaryViewModel(
    string ProjectName,
    string MaterialName,
    string MaterialUnit,
    decimal QuantityRequested,
    decimal? QuantityReceived,
    string Status,
    string CreatedBy,
    DateTimeOffset CreatedOn);
