using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Commands.DismissAll;

public class DismissAllHandler : ICommandHandler<DismissAllCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotifyUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DismissAllHandler(
        INotificationRepository notificationRepository,
        INotifyUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }
    
    public async ValueTask<Result> Handle(DismissAllCommand command, CancellationToken ct)
    {
        var notificationsNotReadedByUser = await _notificationRepository.GetNotificationsByUserNotDismissed(
            _currentUser.Id,
            _currentUser.Permissions.ToList(),
            ct);

        if (notificationsNotReadedByUser.Count <= 0)
            return Result.Success();
        
        notificationsNotReadedByUser.ForEach(n => n.Dismiss(_currentUser.Id));
        
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}