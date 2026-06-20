using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.EventHandlers;

public class NewScheduleNotificationEventHandler : INotificationHandler<NewScheduleNotificationPersistentEvent>
{
    private readonly ISendNotification _sendNotification;

    public NewScheduleNotificationEventHandler(
        ISendNotification sendNotification)
    {
        _sendNotification = sendNotification;
    }
    
    public async ValueTask Handle(NewScheduleNotificationPersistentEvent notification, CancellationToken ct)
    {
        var equipments = string.Join(", ", 
            notification.Equipments?
                .Select(e => new ScheduleEquipmentInput(e.EquipmentId, e.Name))
                .ToList() ?? new List<ScheduleEquipmentInput>());

        var message = $"""
                       Novo agendamento solicitado.

                       Solicitante: {notification.SchedulerName}

                       Data: {notification.Date:dd/MM/yyyy}
                       Horário: {notification.Start:HH:mm} às {notification.End:HH:mm}

                       Equipamentos: {equipments}
                       """;

        await _sendNotification.SendNotification(
            "Agendamento solicitado",
            message,
            notification.ScheduleId.ToString(),
            "Schedule",
            "NewSchedule",
            Permissions.Scheduling.SchedulesManage,
            ct);
    }
}