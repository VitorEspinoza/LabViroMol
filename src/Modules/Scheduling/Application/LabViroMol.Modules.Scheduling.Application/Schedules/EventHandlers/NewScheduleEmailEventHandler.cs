using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.EventHandlers;

public class NewScheduleEmailEventHandler : INotificationHandler<NewScheduleDomainEvent>
{
    private readonly ISendEmail _emailSender;

    public NewScheduleEmailEventHandler(
        ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }
    
    public ValueTask Handle(NewScheduleDomainEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}