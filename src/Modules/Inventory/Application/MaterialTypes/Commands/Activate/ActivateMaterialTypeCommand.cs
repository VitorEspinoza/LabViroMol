using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Activate;

public record ActivateMaterialTypeCommand(MaterialTypeId Id) : ICommand<Result>;
