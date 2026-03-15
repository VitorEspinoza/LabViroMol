using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Orders;

public record OrderProcessing(UserId ProcessedBy, string ProcessedByName, DateTimeOffset ProcessedAt, string? Notes);