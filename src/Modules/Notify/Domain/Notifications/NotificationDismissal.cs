using System.Runtime.InteropServices;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public class NotificationDismissal
{
    public UserId UserId { get; private set; }
    public DateTimeOffset DismissedOn { get; private set; }

    public NotificationDismissal(
        UserId userId,
        DateTimeOffset dismissedOn)
    {
        UserId = userId;
        DismissedOn = dismissedOn;
    }

    public static NotificationDismissal Create(UserId userId)
    {
        return new NotificationDismissal(userId, DateTimeOffset.Now);
    }

}