using LabViroMol.Modules.Shared.Abstractions.Identity;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public class NotificationDismissal
{
    public UserId UserId { get; private set; }
    public DateTimeOffset DismissedOn { get; private set; }
}