namespace LabViroMol.Modules.Notify.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct);
}