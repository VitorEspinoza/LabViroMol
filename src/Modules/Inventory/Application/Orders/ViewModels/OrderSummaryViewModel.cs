namespace LabViroMol.Modules.Inventory.Application.Orders.ViewModels;

public record OrderSummaryViewModel(
    Guid Id,
    Guid MaterialId,
    Guid ProjectId,
    string ProjectName,
    string MaterialName,
    string MaterialUnit,
    decimal QuantityRequested,
    decimal? QuantityReceived,
    string Status,
    string CreatedBy,
    DateTimeOffset CreatedOn);
