using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
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
    
    public async ValueTask Handle(
        NewScheduleDomainEvent notification,
        CancellationToken ct)
    {
        var schedule = notification.Schedule;

        var subject = "Solicitação de Agendamento Recebida";

        var body = $"""
                    <p>Olá, {schedule.Scheduler.Name}.</p>

                    <p>Recebemos sua solicitação de agendamento para o projeto
                    <strong>{schedule.ProjectTitle}</strong>.</p>

                    <p>
                        Data solicitada: {schedule.Scheduling.Date:dd/MM/yyyy}<br />
                        Horário: {schedule.Scheduling.StartDateHour:HH:mm} às
                        {schedule.Scheduling.EndDateHour:HH:mm}
                    </p>

                    <p>
                        Sua solicitação foi registrada e será avaliada pela equipe responsável.
                        Você receberá uma nova notificação quando houver uma definição sobre o agendamento.
                    </p>

                    <p>Atenciosamente,<br />Laboratório de virologia molecular - Hospital de Curitiba UFPR</p>
                    """;

        await _emailSender.SendEmail(
            schedule.Scheduler.Email,
            subject,
            body,
            ct);
    }
}