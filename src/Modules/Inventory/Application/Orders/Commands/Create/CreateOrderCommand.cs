using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Create;

public record CreateOrderCommand(MaterialId MaterialId, ProjectId ProjectId, Quantity Quantity, string description) : ICommand<Result>;
