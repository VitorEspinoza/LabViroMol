using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Start;

public record StartMaintenanceRequestCommand(Guid MaintenanceRequestId) : ICommand<Result>;
