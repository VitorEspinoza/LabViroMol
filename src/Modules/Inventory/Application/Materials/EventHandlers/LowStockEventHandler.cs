using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.EventHandlers;

public sealed class LowStockEventHandler : INotificationHandler<LowStockDomainEvent>
{
    private readonly ISendNotification _sendNotification;
    private readonly IMaterialRepository _materialRepository;

    public LowStockEventHandler(
        ISendNotification sendNotification,
        IMaterialRepository materialRepository)
    {
        _sendNotification = sendNotification;
        _materialRepository = materialRepository;
    }

    public async ValueTask Handle(LowStockDomainEvent notification, CancellationToken ct)
    {
        var message = $"""
                       Material abaixo do estoque mínimo.
                       
                       Material: {notification.MaterialName}
                       Quantidade: {notification.CurrentQuantity}
                       """;

        await _sendNotification.SendNotification(
            "Estoque mínimo",
            message,
            notification.MaterialId.Value.ToString(),
            "Inventory",
            "LowStock",
            Permissions.Inventory.MaterialsManage,
            ct);
    }
}