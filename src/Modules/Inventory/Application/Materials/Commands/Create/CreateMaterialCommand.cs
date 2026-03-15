using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;
using Unit = LabViroMol.Modules.Inventory.Domain.Materials.Unit;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.Create;

public record CreateMaterialCommand(
    string Name,
    string Location,
    Quantity MinStock,
    Quantity StockQuantity,
    Unit Unit,
    MaterialTypeId TypeId) : ICommand<Result>;
