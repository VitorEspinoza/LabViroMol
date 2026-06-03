using System.Collections.Generic;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;

public record CreateScheduleCommand(
    SchedulerInput Scheduler,
    SchedulingInput Scheduling,
    bool AcceptTerm,
    string AdvisorProfessor,
    string ProjectTitle,
    string Description,
    List<ScheduleEquipmentInput> Equipments
) : ICommand<Result>;
