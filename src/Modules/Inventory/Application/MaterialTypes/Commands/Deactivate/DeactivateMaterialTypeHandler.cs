using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Deactivate;

public sealed class DeactivateMaterialTypeHandler : ICommandHandler<DeactivateMaterialTypeCommand, Result>
{
    private readonly IMaterialTypeRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public DeactivateMaterialTypeHandler(
        IMaterialTypeRepository repository,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(DeactivateMaterialTypeCommand command, CancellationToken ct)
    {
        var materialType = await _repository.GetByIdAsync(command.Id, ct);

        if (materialType is null)
            return Result.NotFound("Tipo de material não encontrado.");

        materialType.Deactivate(_currentUser.Id);
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
