using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;

public record CreateMaterialTypeCommand(string Name) : ICommand<Result>;
