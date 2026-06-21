using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Contracts;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Emails.Handlers;

public sealed class CancelScheduleEmailHandler : INotificationHandler<CancelSchedulePersistentEvent>
{
    private readonly ISendEmail _emailSender;

    public CancelScheduleEmailHandler(ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }

    public async ValueTask Handle(
        CancelSchedulePersistentEvent notification,
        CancellationToken cancellationToken)
    {
        var subject = "Agendamento Cancelado";

        var body = MountEmailBody(notification, notification.Justification);

        await _emailSender.SendEmail(
            notification.SchedulerEmail,
            subject,
            body,
            cancellationToken);
    }

    private string MountEmailBody(CancelSchedulePersistentEvent schedule, string justification)
    {
        var body = $"""
                    <p>Olá, {schedule.SchedulerName}.</p>

                    <p>
                        Informamos que sua solicitação de agendamento para o projeto
                        <strong>{schedule.ProjectTitle}</strong> foi <strong>cancelada</strong>.
                    </p>
                    
                    <p>
                        Justificativa: {justification}
                    <p>

                    <h3>Detalhes da solicitação</h3>

                    <ul>
                        <li><strong>Projeto:</strong> {schedule.ProjectTitle}</li>
                        <li><strong>Professor Orientador:</strong> {schedule.AdvisorProfessor}</li>
                        <li><strong>Data solicitada:</strong> {schedule.Date:dd/MM/yyyy}</li>
                        <li><strong>Horário:</strong> {schedule.Start:HH:mm} às {schedule.End:HH:mm}</li>
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