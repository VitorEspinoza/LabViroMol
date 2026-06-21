using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Handlers;

public class CancelScheduleEventHandler : INotificationHandler<CanceledScheduleDomainEvent>
{
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public CancelScheduleEventHandler(
        ISchedulingUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public ValueTask Handle(CanceledScheduleDomainEvent notification, CancellationToken cancellationToken)
    {
        var persistentEvent = new CancelSchedulePersistentEvent(
            notification.Schedule.Scheduler.Email,
            notification.Schedule.Scheduler.Name,
            notification.Schedule.ProjectTitle,
            notification.Schedule.AdvisorProfessor,
            notification.Schedule.Scheduling.Date,
            notification.Schedule.Scheduling.StartDateHour,
            notification.Schedule.Scheduling.EndDateHour,
            notification.Justification);
        
        _unitOfWork.AddPersistentEvent(persistentEvent);
        
        return ValueTask.CompletedTask;
    }
}