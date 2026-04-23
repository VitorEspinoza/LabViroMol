using System.Windows.Input;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Start;

public record StartMaintenanceRequestCommand(Guid MaintenanceRequestId) : ICommand<Result>;