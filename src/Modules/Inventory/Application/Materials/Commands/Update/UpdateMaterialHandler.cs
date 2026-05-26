using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.Update;

public class UpdateMaterialHandler : ICommandHandler<UpdateMaterialCommand, Result>
{
    private readonly IMaterialRepository _repository;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public UpdateMaterialHandler(
        IMaterialRepository repository,
        IInventoryUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(UpdateMaterialCommand command, CancellationToken ct)
    {
        var material = await _repository.GetByIdAsync(MaterialId.From(command.MaterialId), ct);

        if (material is null)
            return Result.NotFound("Material não encontrado.");

        material.Update(
            command.Name,
            command.MinStock,
            command.Location);

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
