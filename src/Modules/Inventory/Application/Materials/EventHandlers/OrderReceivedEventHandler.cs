using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.EventHandlers;

public class OrderReceivedEventHandler : INotificationHandler<OrderReceivedDomainEvent>
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IInventoryUnitOfWork _unitOfWork;
    
    public OrderReceivedEventHandler(IMaterialRepository materialRepository, IInventoryUnitOfWork unitOfWork)
    {
        _materialRepository = materialRepository;
        _unitOfWork = unitOfWork;
    }
    public async ValueTask Handle(OrderReceivedDomainEvent notification, CancellationToken cancellationToken)
    {
        var material = await _materialRepository.GetByIdAsync(notification.MaterialId, cancellationToken);

        material?.ReceiveFromOrder(notification.OrderId, notification.QuantityReceived, notification.ReceivedBy);
        
        await _unitOfWork.CompleteAsync(cancellationToken);
    }
}