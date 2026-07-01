using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Cancel;

public record CancelScheduleCommand(ScheduleId ScheduleId, string Justification) : ICommand<Result>;