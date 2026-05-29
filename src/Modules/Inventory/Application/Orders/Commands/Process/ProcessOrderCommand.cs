using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Process;

public record ProcessOrderCommand(
    OrderId OrderId,
    string? Notes
) : ICommand<Result>;
