using System;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Orders;

public record OrderProcessing(UserId ProcessedBy, string ProcessedByName, DateTimeOffset ProcessedAt, string? Notes);