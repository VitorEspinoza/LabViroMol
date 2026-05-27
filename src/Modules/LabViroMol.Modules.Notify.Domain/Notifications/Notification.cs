using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public class Notification : AggregateRoot<NotificationId>
{
    private Notification(){}

    private Notification(NotificationId id, string title, string message, Guid targetPermissionId) : base(id)
    {
        Title = title;
        Message = message;
        TargetPermissionId = targetPermissionId;
        ExpiresOn = CreatedAt.AddDays(3.0);
    }
    
    public string Title { get; private set; }
    public string Message { get; private set; }
    public Guid TargetPermissionId { get; private set; }
    public DateTimeOffset ExpiresOn  { get; private set; }
    
    public List<NotificationDismissal>  NotificationDismissals { get; private set; }
}