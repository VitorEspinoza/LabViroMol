using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.FixDetails;

public record FixOrderDetailsCommand(
    OrderId OrderId,
    ProjectId NewProjectId,
    Quantity NewQuantity,
    string Description) : ICommand<Result>;
