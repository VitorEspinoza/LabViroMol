using System.Windows.Input;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Done;

public record DoneMaintenanceRequestCommand(Guid MaintenanceRequestId) : ICommand<Result>;