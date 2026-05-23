using LabViroMol.Modules.Scheduling.Domain.Schedules;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;

public record SchedulerInput(string Name, string Course, string Email);
