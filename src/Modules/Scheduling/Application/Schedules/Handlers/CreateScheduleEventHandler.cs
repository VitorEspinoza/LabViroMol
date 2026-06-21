using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Handlers;

public class CreateScheduleEventHandler : INotificationHandler<NewScheduleDomainEvent>
{
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public CreateScheduleEventHandler(
        ISchedulingUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public ValueTask Handle(NewScheduleDomainEvent notification, CancellationToken cancellationToken)
    {
        var persistentEvent = new CreateScheduleEmailPersistentEvent(
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