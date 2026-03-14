using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Deactivate;

public record DeactivateMaterialTypeCommand(MaterialTypeId Id) : ICommand<Result>;
