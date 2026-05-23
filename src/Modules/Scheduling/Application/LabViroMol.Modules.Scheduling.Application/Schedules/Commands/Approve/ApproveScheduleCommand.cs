using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;

public record ApproveScheduleCommand(ScheduleId ScheduleId) : ICommand<Result>;