using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Contracts;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Emails.Handlers;

public sealed class ApproveScheduleEmailHandler : INotificationHandler<ApproveSchedulePersistentEvent>
{
    private readonly ISendEmail _emailSender;

    public ApproveScheduleEmailHandler(ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }

    public async ValueTask Handle(
        ApproveSchedulePersistentEvent notification,
        CancellationToken ct)
    {
        var subject = "Agendamento Confirmado";

        var body = MountEmailBody(notification);

        await _emailSender.SendEmail(
            notification.SchedulerEmail,
            subject,
            body,
            ct);
    }

    private string MountEmailBody(ApproveSchedulePersistentEvent schedule)
    {
        var body = $"""
                    <p>Olá, {schedule.SchedulerName}.</p>

                    <p>
                        Temos o prazer de informar que sua solicitação de agendamento foi
                        <strong>aprovada</strong>.
                    </p>

                    <h3>Detalhes do agendamento</h3>

                    <ul>
                        <li><strong>Projeto:</strong> {schedule.ProjectTitle}</li>
                        <li><strong>Professor Orientador:</strong> {schedule.AdvisorProfessor}</li>
                        <li><strong>Data:</strong> {schedule.Date:dd/MM/yyyy}</li>
                        <li><strong>Horário:</strong> {schedule.Start:HH:mm} às {schedule.End:HH:mm}</li>
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
        
        return body;
    }
}