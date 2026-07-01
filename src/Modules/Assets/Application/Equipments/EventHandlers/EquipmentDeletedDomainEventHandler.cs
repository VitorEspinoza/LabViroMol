using LabViroMol.Modules.Assets.Domain.Equipments.Events;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.EventHandlers;

public sealed class EquipmentDeletedDomainEventHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository)
    : INotificationHandler<EquipmentDeletedDomainEvent>
{
    public async ValueTask Handle(EquipmentDeletedDomainEvent notification, CancellationToken ct)
    {
        var maintenanceRequests = await maintenanceRequestRepository
            .GetAllByEquipmentIdAsync(notification.EquipmentId, ct);

        foreach (var maintenanceRequest in maintenanceRequests)
        {
            maintenanceRequestRepository.Remove(maintenanceRequest);
        }
    }
}
