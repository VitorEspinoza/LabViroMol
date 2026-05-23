using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Refuse;

public record RefuseScheduleCommand(ScheduleId ScheduleId) : ICommand<Result>;