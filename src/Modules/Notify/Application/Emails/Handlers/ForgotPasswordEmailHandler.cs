using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Notify.Contracts;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Emails.Handlers;

public class ForgotPasswordEmailHandler : INotificationHandler<ForgotPasswordPersistentEvent>
{
    private readonly ISendEmail _emailSender;

    public ForgotPasswordEmailHandler(
        ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }
    
    public async ValueTask Handle(ForgotPasswordPersistentEvent notification, CancellationToken ct)
    {
        await _emailSender.SendEmail(notification.Email, notification.Subject, notification.Body, ct);
    }
}