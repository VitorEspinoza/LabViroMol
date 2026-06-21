using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Handlers;

public class ApproveScheduleEventHandler : INotificationHandler<ApprovedScheduleDomainEvent>
{
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public ApproveScheduleEventHandler(
        ISchedulingUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public ValueTask Handle(ApprovedScheduleDomainEvent notification, CancellationToken cancellationToken)
    {
        var persistentEvent = new ApproveSchedulePersistentEvent(
            notification.Schedule.Scheduler.Email,
            notification.Schedule.Scheduler.Name,
            notification.Schedule.ProjectTitle,
            notification.Schedule.AdvisorProfessor,
            notification.Schedule.Scheduling.Date,
            notification.Schedule.Scheduling.StartDateHour,
            notification.Schedule.Scheduling.EndDateHour);
        
        _unitOfWork.AddPersistentEvent(persistentEvent);
        
        return ValueTask.CompletedTask;
    }
}