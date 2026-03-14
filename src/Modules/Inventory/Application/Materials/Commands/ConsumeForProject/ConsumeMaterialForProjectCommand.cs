using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.ConsumeForProject;

public record ConsumeMaterialForProjectCommand(MaterialId MaterialId, Quantity Quantity, ProjectId ProjectId) : ICommand<Result>;
