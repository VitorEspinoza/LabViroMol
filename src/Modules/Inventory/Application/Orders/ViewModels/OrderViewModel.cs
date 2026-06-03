using System;

namespace LabViroMol.Modules.Inventory.Application.Orders.ViewModels;

public record OrderViewModel(
    Guid MaterialId,
    string MaterialName,
    Guid ProjectId,
    string ProjectName,
    string Status,
    string Description,
    decimal RequestedQuantity,
    string? ProcessedBy,
    DateTimeOffset? ProcessedOn,
    string? ProcessingNotes,
    string? ReceivedBy,
    DateTimeOffset? ReceivedOn,
    decimal? ReceivedQuantity,
    string? ReceiptNotes
);
