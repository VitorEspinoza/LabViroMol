namespace LabViroMol.Modules.Inventory.Application.Orders.ViewModels;

public record OrderViewModel(
    Guid Id,
    Guid MaterialId,
    string MaterialName,
    Guid ProjectId,
    string ProjectName,
    string Status,
    string Description,
    decimal RequestedQuantity,
    string RequestedByName,
    DateTimeOffset RequestedOn,
    string? ProcessedByName,
    DateTimeOffset? ProcessedOn,
    string? ProcessingNotes,
    string? ReceivedByName,
    DateTimeOffset? ReceivedOn,
    decimal? ReceivedQuantity,
    string? ReceiptNotes
);
