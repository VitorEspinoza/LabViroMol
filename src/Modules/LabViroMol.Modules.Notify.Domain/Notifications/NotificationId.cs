using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public record struct NotificationId(Guid Value) : IStrongId<NotificationId>
{
    public static NotificationId From(Guid value) => new(value);
    public static implicit operator Guid(NotificationId id) => id.Value;
}