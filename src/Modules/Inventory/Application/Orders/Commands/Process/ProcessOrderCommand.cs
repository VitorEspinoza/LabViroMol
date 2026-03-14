using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Process;

public record ProcessOrderCommand(
    OrderId OrderId,
    string? Notes
) : ICommand<Result>;
