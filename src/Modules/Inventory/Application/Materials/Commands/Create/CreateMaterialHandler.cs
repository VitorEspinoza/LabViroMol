using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.Create;

public class CreateMaterialHandler : ICommandHandler<CreateMaterialCommand, Result>
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IMaterialTypeRepository _materialTypeRepository;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public CreateMaterialHandler(
        IMaterialRepository materialRepository,
        IMaterialTypeRepository materialTypeRepository,
        IInventoryUnitOfWork unitOfWork)
    {
        _materialRepository = materialRepository;
        _materialTypeRepository = materialTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(CreateMaterialCommand command, CancellationToken ct)
    {
        var type = await _materialTypeRepository.GetByIdAsync(command.TypeId, ct);

        if (type is null)
            return Result.NotFound("Tipo de material não encontrado.");

        var result = Material.Create(
            command.Name,
            command.Location,
            command.MinStock,
            command.StockQuantity,
            command.Unit,
            type);

        if (result.IsFailure)
            return result;

        await _materialRepository.AddAsync(result.Data!, ct);
        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
