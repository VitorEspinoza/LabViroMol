using System;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Done;

public record DoneMaintenanceRequestCommand(Guid MaintenanceRequestId) : ICommand<Result>;
