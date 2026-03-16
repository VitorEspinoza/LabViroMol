using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;

public record CreateScheduleCommand(
    SchedulerInput Scheduler,
    SchedulingInput Scheduling,
    bool AcceptTerm,
    string AdvisorProfessor,
    string ProjectTitle,
    string Description
) : ICommand<Result>;
