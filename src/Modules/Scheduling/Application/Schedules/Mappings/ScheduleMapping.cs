using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;
using LabViroMol.Modules.Scheduling.Domain.Schedules;


namespace LabViroMol.Modules.Scheduling.Application.Schedules.Mappings;

public static class ScheduleMapper
{
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
            sched.Status.ToString(),
            sched.TermUrl,
            sched.Equipments
                .Select(e => new ScheduleEquipmentViewModel(e.EquipmentId, e.Name))
                .ToList());
    }
}