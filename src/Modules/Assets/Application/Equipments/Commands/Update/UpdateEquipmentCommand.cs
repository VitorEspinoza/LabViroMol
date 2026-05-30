using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;

public record UpdateEquipmentCommand(EquipmentId EquipmentId, string Name, string Model, string Brand, string Code, string Description) : ICommand<Result>;
