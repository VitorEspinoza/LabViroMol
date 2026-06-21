using LabViroMol.Modules.Notify.Application.Notifications.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Notify.Application.Notifications.Queries;

public interface INotificationQueries
{
    Task<List<NotificationViewModel>> GetUnreadByUserAsync(UserId userId, List<string> permissions, CancellationToken ct);
}
