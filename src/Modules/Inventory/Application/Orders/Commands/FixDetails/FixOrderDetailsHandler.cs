using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.FixDetails;

public class FixOrderDetailsHandler : ICommandHandler<FixOrderDetailsCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProjectChecker _projectChecker;
    private readonly IInventoryUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FixOrderDetailsHandler(IOrderRepository orderRepository, IProjectChecker projectChecker, IInventoryUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _projectChecker = projectChecker;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async ValueTask<Result> Handle(FixOrderDetailsCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);

        if (order is null)
            return Result.NotFound("Pedido não encontrado.");

        var isEligibleForOrdersResult = await _projectChecker.IsEligibleForOrdersAsync(command.NewProjectId, ct);
        if (isEligibleForOrdersResult.IsFailure)
            return isEligibleForOrdersResult;

        var result = order.FixDetails(
            command.NewProjectId,
            command.NewQuantity,
            command.Description,
            _currentUser.Id);

        if (result.IsFailure)
            return result;

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
