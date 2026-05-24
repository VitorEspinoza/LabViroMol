using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Cancel;

public class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);

        if (order is null)
            return Result.NotFound("Pedido não encontrado.");

        var result = order.Cancel(_currentUser.Id);

        if (result.IsFailure)
            return result;

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
