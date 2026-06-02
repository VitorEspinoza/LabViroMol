

using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public class Notification : AggregateRoot<NotificationId>, ICreationAuditable
{
    private Notification(){}

    private Notification(
        NotificationId id, 
        string title, 
        string message, 
        string referenceId, 
        string referenceModule, 
        string type, 
        string targetPermission, 
        DateTimeOffset expiresOn) : base(id)
    {
        Title = title;
        Message = message;
        ReferenceId = referenceId;
        ReferenceModule = referenceModule;
        Type = type;
        TargetPermission = targetPermission;
        ExpiresOn = expiresOn;
    }
    
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string ReferenceId { get; private set; }
    public string ReferenceModule { get; private set; }
    public string Type { get; private set; }
    public string TargetPermission { get; private set; }
    public DateTimeOffset ExpiresOn  { get; private set; }
    
    
    
    public List<NotificationDismissal> NotificationDismissals { get; private set; } = [];

    public static Result<Notification> Create(string title, string message, string targetPermission, string referenceId, string referenceModule, string type)
    {
        var creationDate = DateTimeOffset.Now;
        var notification = new Notification(
            IdFactory.New<NotificationId>(),
            title,
            message,
            referenceId,
            referenceModule,
            type,
            targetPermission,
            creationDate.AddDays(3));
        
        return Result<Notification>.Success(notification);
    }

    public void Dismiss(UserId userId)
    {   
        NotificationDismissals ??= [];
        
        var alreadyDismissed = NotificationDismissals
            .Any(d => d.UserId == userId);

        if (alreadyDismissed) return;
        
        var dismissal = NotificationDismissal.Create(userId);
        
        NotificationDismissals.Add(dismissal);
    }
}