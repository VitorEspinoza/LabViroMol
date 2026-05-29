using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Delete;

public record DeleteEquipmentCommand(EquipmentId EquipmentId) : ICommand<Result>;
