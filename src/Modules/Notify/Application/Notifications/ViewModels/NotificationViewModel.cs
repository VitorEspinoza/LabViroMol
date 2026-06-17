namespace LabViroMol.Modules.Notify.Application.Notifications.ViewModels;

public record NotificationViewModel(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string ReferenceId,
    string ReferenceModule,
    DateTimeOffset CreatedAt);
