using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;

public record CreateMaintenanceCommand(Guid EquipmentId, string Description, string ProblemDescription) : ICommand<Result>;
