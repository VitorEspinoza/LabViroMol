using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Commands.Dismiss;

public sealed class DismissHandler : ICommandHandler<DismissCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotifyUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DismissHandler(
        INotificationRepository notificationRepository,
        INotifyUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async ValueTask<Result> Handle(DismissCommand command, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByNotificationId(command.NotificationId, ct);

        if (notification == null)
            return Result.BusinessRule("Notificação não encontrada.");

        notification.Dismiss(_currentUser.Id);
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}