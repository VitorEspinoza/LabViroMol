using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Commands.Dismiss;

public record DismissCommand(NotificationId NotificationId) : ICommand<Result>;