using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.EventHandlers;

public class LowStockEventHandler : INotificationHandler<LowStockDomainEvent>
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
        var material = await _materialRepository.GetByIdAsync(notification.MaterialId, ct);
        
        var materialName = material?.Name ?? string.Empty;

        var message = $"""
                       Material abaixo do estoque mínimo.
                       
                       Material: {materialName}
                       Quantidade: {notification.CurrentQuantity}
                       """;

        await _sendNotification.SendNotification(
            "Estoque mínimo",
            message,
            material?.Id.ToString(),
            "Inventory",
            "LowStock",
            Permissions.Inventory.MaterialsManage,
            ct);
    }
}