using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;

public record CreateMaintenanceCommand(EquipmentId EquipmentId, string Description, string ProblemDescription) : ICommand<Result>;