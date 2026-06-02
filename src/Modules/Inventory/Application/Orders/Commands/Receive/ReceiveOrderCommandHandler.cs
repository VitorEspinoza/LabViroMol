using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Receive;

public class ReceiveOrderCommandHandler : ICommandHandler<ReceiveOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public ReceiveOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(ReceiveOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);

        if (order is null)
            return Result.NotFound("Pedido não encontrado.");

        var result = order.Receive(_currentUser.Id, _currentUser.FullName, command.QuantityReceived, command.Notes);

        if (result.IsFailure)
            return result;

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
