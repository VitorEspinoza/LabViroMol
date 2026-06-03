using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Commands.DismissBatch;

public class DismissBatchHandler : ICommandHandler<DismissBatchCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotifyUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DismissBatchHandler(
        INotificationRepository notificationRepository,
        INotifyUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }
    
    public async ValueTask<Result> Handle(DismissBatchCommand command, CancellationToken ct)
    {
        var notifications = await _notificationRepository.GetAllByNotificationIds(command.NotificationIds, ct);
        notifications.ForEach(n => n.Dismiss(_currentUser.Id));
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}