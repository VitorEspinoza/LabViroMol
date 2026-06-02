using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.EventHandlers;

public class NewScheduleEventHandler : INotificationHandler<NewScheduleDomainEvent>
{
    private readonly ISendNotification _sendNotification;

    public NewScheduleEventHandler(
        ISendNotification sendNotification)
    {
        _sendNotification = sendNotification;
    }
    
    public async ValueTask Handle(NewScheduleDomainEvent notification, CancellationToken ct)
    {
        var schedule = notification.Schedule;
        var equipments = string.Join(", ", 
            schedule.Equipments.Select(e => e.Name));

        var message = $"""
                       Novo agendamento solicitado.

                       Solicitante: {schedule.Scheduler.Name}

                       Data: {schedule.Scheduling.Date:dd/MM/yyyy}
                       Horário: {schedule.Scheduling.StartDateHour:HH:mm} às {schedule.Scheduling.EndDateHour:HH:mm}

                       Equipamentos: {equipments}
                       """;

        await _sendNotification.SendNotification(
            "Agendamento solicitado",
            message,
            schedule.Id.ToString(),
            "Schedule",
            "NewSchedule",
            Permissions.Scheduling.SchedulesManage,
            ct);
    }
}