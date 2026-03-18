using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;
using LabViroMol.Modules.Scheduling.Domain.Schedules;


namespace LabViroMol.Modules.Scheduling.Application.Schedules.Mappings;

public static class ScheduleMapper
{
    public static Scheduler ToEntity(SchedulerInput schedulerInput)
    {
        return new Scheduler(
            schedulerInput.Name,
            schedulerInput.Course,
            schedulerInput.Email);
    }

    public static Domain.Schedules.Scheduling ToEntity(SchedulingInput schedulingInput)
    {
        return new Domain.Schedules.Scheduling(
            schedulingInput.Date,
            schedulingInput.Start,
            schedulingInput.End);
    }

    public static ScheduleViewModel FromEntity(Schedule sched)
    {
        return new ScheduleViewModel(
            sched.Id.Value,
            new SchedulerViewModel(
                sched.Scheduler.Name,
                sched.Scheduler.Course,
                sched.Scheduler.Email),
            new SchedulingViewModel(
                sched.Scheduling.Date,
                sched.Scheduling.StartDateHour,
                sched.Scheduling.EndDateHour),
            sched.ProjectTitle,
            sched.Description,
            sched.AdvisorProfessor,
            sched.Status.ToString());
    }
}