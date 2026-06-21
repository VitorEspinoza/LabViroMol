using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Contracts;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Emails.Handlers;

public sealed class CreateScheduleEmailHandler : INotificationHandler<CreateScheduleEmailPersistentEvent>
{
    private readonly ISendEmail _emailSender;

    public CreateScheduleEmailHandler(
        ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }
    
    public async ValueTask Handle(
        CreateScheduleEmailPersistentEvent notification,
        CancellationToken ct)
    {

        var subject = "Solicitação de Agendamento Recebida";

        var body = MountEmailBody(notification);

        await _emailSender.SendEmail(
            notification.SchedulerEmail,
            subject,
            body,
            ct);
    }

    private string MountEmailBody(CreateScheduleEmailPersistentEvent notification)
    {
        var body = $"""
                    <p>Olá, {notification.SchedulerName}.</p>

                    <p>Recebemos sua solicitação de agendamento para o projeto
                    <strong>{notification.ProjectTitle}</strong>.</p>

                    <p>
                        Data solicitada: {notification.Date:dd/MM/yyyy}<br />
                        Horário: {notification.Start:HH:mm} às
                        {notification.End:HH:mm}
                    </p>

                    <p>
                        Sua solicitação foi registrada e será avaliada pela equipe responsável.
                        Você receberá uma nova notificação quando houver uma definição sobre o agendamento.
                    </p>

                    <p>Atenciosamente,<br />Laboratório de virologia molecular - Hospital de Curitiba UFPR</p>
                    """;

        return body;
    }
}