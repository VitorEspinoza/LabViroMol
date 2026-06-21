using LabViroMol.Modules.Notify.Application.Notifications.ViewModels;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Handlers;

public class CreateScheduleNotificationHandler : INotificationHandler<CreateScheduleNotificationPersistentEvent>
{
    private readonly ISendNotification _sendNotification;

    public CreateScheduleNotificationHandler(
        ISendNotification sendNotification)
    {
        _sendNotification = sendNotification;
    }
    
    public async ValueTask Handle(CreateScheduleNotificationPersistentEvent notification, CancellationToken ct)
    {
        var equipments = string.Join(", ", 
            notification.Equipments?
                .Select(e => new ScheduleEquipmentViewModel(e.EquipmentId, e.Name))
                .ToList() ?? new List<ScheduleEquipmentViewModel>());

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