using LabViroMol.Modules.Inventory.Application.Materials.Commands.AddStockException;
using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.AddStock;

public class AddStockMaterialExceptionHandler : ICommandHandler<AddStockMaterialExceptionCommand, Result>
{
    private readonly IMaterialRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public AddStockMaterialExceptionHandler(
        IMaterialRepository repository,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(AddStockMaterialExceptionCommand exceptionCommand, CancellationToken ct)
    {
        var material = await _repository.GetByIdAsync(MaterialId.From(exceptionCommand.MaterialId), ct);

        if (material is null)
            return Result.NotFound("Material não encontrado.");

        material.AddStockException(exceptionCommand.Quantity, exceptionCommand.Reason, _currentUser.Id);
        
        
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
