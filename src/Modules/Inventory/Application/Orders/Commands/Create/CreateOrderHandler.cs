using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Create;

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProjectChecker _projectChecker;
    private readonly IMaterialRepository _materialRepository;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IProjectChecker projectChecker,
        IMaterialRepository materialRepository,
        IInventoryUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _projectChecker = projectChecker;
        _materialRepository = materialRepository;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var material = await _materialRepository.GetByIdAsync(command.MaterialId, ct);

        if (material is null)
            return Result.NotFound("Material não encontrado.");

        var isEligibleForOrdersResult = await _projectChecker.IsEligibleForOrdersAsync(command.ProjectId, ct);

        if (isEligibleForOrdersResult.IsFailure)
            return isEligibleForOrdersResult;


        var order = Order.Create(
            command.MaterialId,
            command.ProjectId,
            command.Quantity,
            command.description);

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
