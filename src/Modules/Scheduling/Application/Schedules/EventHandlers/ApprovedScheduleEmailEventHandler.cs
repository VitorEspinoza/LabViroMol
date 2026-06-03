using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.EventHandlers;

public class ApprovedScheduleEmailEventHandler : INotificationHandler<ApprovedScheduleDomainEvent>
{
    private readonly ISendEmail _emailSender;

    public ApprovedScheduleEmailEventHandler(ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }

    public async ValueTask Handle(
        ApprovedScheduleDomainEvent notification,
        CancellationToken ct)
    {
        var schedule = notification.Schedule;

        var subject = "Agendamento Confirmado";

        var body = $"""
                    <p>Olá, {schedule.Scheduler.Name}.</p>

                    <p>
                        Temos o prazer de informar que sua solicitação de agendamento foi
                        <strong>aprovada</strong>.
                    </p>

                    <h3>Detalhes do agendamento</h3>

                    <ul>
                        <li><strong>Projeto:</strong> {schedule.ProjectTitle}</li>
                        <li><strong>Professor Orientador:</strong> {schedule.AdvisorProfessor}</li>
                        <li><strong>Data:</strong> {schedule.Scheduling.Date:dd/MM/yyyy}</li>
                        <li><strong>Horário:</strong> {schedule.Scheduling.StartDateHour:HH:mm} às {schedule.Scheduling.EndDateHour:HH:mm}</li>
                    </ul>

                    <p>
                        Solicitamos que compareça no horário agendado e siga as orientações
                        de utilização do laboratório e dos equipamentos reservados.
                    </p>

                    <p>
                        Em caso de dúvidas ou necessidade de alteração, entre em contato com a equipe responsável.
                    </p>

                    <p>
                        Atenciosamente,<br />
                        Laboratório de Virologia Molecular - Hospital de Curitiba UFPR
                    </p>
                    """;

        await _emailSender.SendEmail(
            schedule.Scheduler.Email,
            subject,
            body,
            ct);
    }
}