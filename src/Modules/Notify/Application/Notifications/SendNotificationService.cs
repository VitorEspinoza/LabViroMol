using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Application.Notifications;

internal class SendNotificationService : ISendNotification
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotifyUnitOfWork _unitOfWork;

    public SendNotificationService(
        INotificationRepository notificationRepository,
        INotifyUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result> SendNotification(
        string title, 
        string message, 
        string referenceId, 
        string referenceModule, 
        string type, 
        string permissionId, 
        CancellationToken ct)
    {
        var result = Notification.Create(
            title,
            message,
            permissionId,
            referenceId,
            referenceModule,
            type);

        if (result.IsFailure)
            return result;
        
        await _notificationRepository.AddAsync(result.Data!, ct);
        await _unitOfWork.CompleteAsync(ct);
        
        return Result.Success();
    }
}