using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Orders;

public record OrderReceipt(UserId ReceivedBy, string ReceivedByName, string? Notes, Quantity Quantity, DateTimeOffset ReceivedAt);
