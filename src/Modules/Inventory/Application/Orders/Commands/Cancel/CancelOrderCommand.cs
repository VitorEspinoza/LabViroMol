using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Cancel;

public record CancelOrderCommand(OrderId OrderId) : ICommand<Result>;
