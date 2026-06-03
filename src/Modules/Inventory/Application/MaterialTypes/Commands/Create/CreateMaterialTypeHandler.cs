using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
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
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
