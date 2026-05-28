using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Cancel;

public record CancelMaintenanceRequestCommand(MaintenanceRequestId MaintenanceRequestId) : ICommand<Result>;