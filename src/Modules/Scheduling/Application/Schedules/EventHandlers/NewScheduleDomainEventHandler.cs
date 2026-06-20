using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.EventHandlers;

public class NewScheduleDomainEventHandler : INotificationHandler<NewScheduleDomainEvent>
{
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public NewScheduleDomainEventHandler(
        ISchedulingUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public ValueTask Handle(NewScheduleDomainEvent notification, CancellationToken cancellationToken)
    {
        var persistentEvent = new NewScheduleEmailPersistentEvent(
            notification.Schedule.Scheduler.Email,
            notification.Schedule.Scheduler.Name,
            notification.Schedule.ProjectTitle,
            notification.Schedule.Scheduling.Date,
            notification.Schedule.Scheduling.StartDateHour,
            notification.Schedule.Scheduling.EndDateHour);
        
        _unitOfWork.AddPersistentEvent(persistentEvent);
        
        return ValueTask.CompletedTask;
    }
}