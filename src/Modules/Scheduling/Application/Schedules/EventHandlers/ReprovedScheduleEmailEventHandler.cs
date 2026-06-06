using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.EventHandlers;

public class ReprovedScheduleEmailEventHandler : INotificationHandler<ReprovedScheduleDomainEvent>
{
    private readonly ISendEmail _emailSender;

    public ReprovedScheduleEmailEventHandler(ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }

    public async ValueTask Handle(
        ReprovedScheduleDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var schedule = notification.Schedule;

        var subject = "Solicitação de Agendamento Não Aprovada";

        var body = MountEmailBody(schedule, notification.Justification);

        await _emailSender.SendEmail(
            schedule.Scheduler.Email,
            subject,
            body,
            cancellationToken);
    }

    private string MountEmailBody(Schedule schedule, string justification)
    {
        var body = $"""
                    <p>Olá, {schedule.Scheduler.Name}.</p>

                    <p>
                        Informamos que sua solicitação de agendamento para o projeto
                        <strong>{schedule.ProjectTitle}</strong> foi analisada e,
                        neste momento, <strong>não foi aprovada</strong>.
                    </p>
                    
                    <p>
                        Justificativa: {justification}
                    <p>

                    <h3>Detalhes da solicitação</h3>

                    <ul>
                        <li><strong>Projeto:</strong> {schedule.ProjectTitle}</li>
                        <li><strong>Professor Orientador:</strong> {schedule.AdvisorProfessor}</li>
                        <li><strong>Data solicitada:</strong> {schedule.Scheduling.Date:dd/MM/yyyy}</li>
                        <li><strong>Horário:</strong> {schedule.Scheduling.StartDateHour:HH:mm} às {schedule.Scheduling.EndDateHour:HH:mm}</li>
                    </ul>

                    <p>
                        Caso necessário, você poderá realizar uma nova solicitação de agendamento
                        considerando a disponibilidade do laboratório e dos equipamentos.
                    </p>

                    <p>
                        Em caso de dúvidas, entre em contato com a equipe responsável.
                    </p>

                    <p>
                        Atenciosamente,<br />
                        Laboratório de Virologia Molecular - Hospital de Curitiba UFPR
                    </p>
                    """;
        
        return body;
    }
}