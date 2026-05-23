using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Cancel;

public record CancelMaintenanceRequestCommand(MaintenanceRequestId MaintenanceRequestId) : ICommand<Result>;