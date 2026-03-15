using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Receive;

public record ReceiveOrderCommand(
    OrderId OrderId,
    Quantity QuantityReceived,
    string? Notes
) : ICommand<Result>;
