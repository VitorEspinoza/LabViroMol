using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Notify.Contracts;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Emails.Handlers;

public sealed class ResetPasswordEmailHandler : INotificationHandler<ResetPasswordPersistentEvent>
{
    private readonly ISendEmail _emailSender;

    public ResetPasswordEmailHandler(
        ISendEmail emailSender)
    {
        _emailSender = emailSender;
    }
    
    public async ValueTask Handle(ResetPasswordPersistentEvent notification, CancellationToken ct)
    {
        await _emailSender.SendEmail(notification.Email, notification.Subject, notification.Body, ct);
    }
}