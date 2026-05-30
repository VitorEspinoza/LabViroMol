using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.Update;

public record UpdateMaterialCommand(
    MaterialId MaterialId,
    string Name,
    string Location,
    Quantity MinStock) : ICommand<Result>;
