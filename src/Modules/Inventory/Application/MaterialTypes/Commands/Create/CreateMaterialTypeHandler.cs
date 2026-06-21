using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Kernel.Exceptions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Create;

public class CreateMaterialTypeHandler : ICommandHandler<CreateMaterialTypeCommand, Result>
{
    private readonly IMaterialTypeRepository _repository;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public CreateMaterialTypeHandler(
        IMaterialTypeRepository repository,
        IInventoryUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(CreateMaterialTypeCommand command, CancellationToken ct)
    {
        var materialType = MaterialType.Create(command.Name);
        await _repository.AddAsync(materialType, ct);

        try
        {
            await _unitOfWork.CompleteAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Conflict("Já existe um tipo de material com este nome.");
        }

        return Result.Success();
    }
}
