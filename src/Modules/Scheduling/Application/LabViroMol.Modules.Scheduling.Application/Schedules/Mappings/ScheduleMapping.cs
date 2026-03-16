using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;


namespace LabViroMol.Modules.Scheduling.Application.Schedules.Mappings;

public static class ScheduleMapper
{
    public static Scheduler ToEntity(SchedulerInput schedulerInput)
    {
        return new Scheduler(
            schedulerInput.Name,
            schedulerInput.Course,
            schedulerInput.Name);
    }

    public static Domain.Schedules.Scheduling ToEntity(SchedulingInput schedulingInput)
    {
        return new Domain.Schedules.Scheduling(
            schedulingInput.Date,
            schedulingInput.Start,
            schedulingInput.End);
    }
}