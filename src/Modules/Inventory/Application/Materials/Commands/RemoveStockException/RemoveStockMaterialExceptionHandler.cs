using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.RemoveStockException;

public class RemoveStockMaterialExceptionHandler : ICommandHandler<RemoveStockMaterialExceptionCommand, Result>
{
    private readonly IMaterialRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public RemoveStockMaterialExceptionHandler(
        IMaterialRepository repository,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(RemoveStockMaterialExceptionCommand command, CancellationToken ct)
    {
        var material = await _repository.GetByIdAsync(MaterialId.From(command.MaterialId), ct);

        if (material is null)
            return Result.NotFound("Material não encontrado.");

        var result = material.RemoveStockException(command.Quantity, command.Reason, _currentUser.Id);

        if (result.IsFailure)
            return result;

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
