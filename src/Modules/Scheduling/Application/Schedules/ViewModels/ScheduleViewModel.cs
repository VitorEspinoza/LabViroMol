using System;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;

public record ScheduleViewModel(Guid Id, SchedulerViewModel Scheduler, SchedulingViewModel Scheduling, string ProjectTitle, string Description, string AdvisorProfessor, string Status);