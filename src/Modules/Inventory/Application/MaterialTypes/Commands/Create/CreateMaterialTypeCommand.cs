using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;

public record CreateMaterialTypeCommand(string Name) : ICommand<Result>;
