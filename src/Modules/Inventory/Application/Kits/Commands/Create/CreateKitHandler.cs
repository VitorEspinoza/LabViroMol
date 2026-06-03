using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Kits.Commands.Create;

public class CreateKitHandler : ICommandHandler<CreateKitCommand, Result>
{
    private readonly IKitRepository _kitRepository;
    private readonly MaterialValidatorService _materialValidatorService;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public CreateKitHandler(
        IKitRepository kitRepository,
        MaterialValidatorService materialValidatorService,
        IInventoryUnitOfWork unitOfWork)
    {
        _kitRepository = kitRepository;
        _materialValidatorService = materialValidatorService;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(CreateKitCommand command, CancellationToken ct)
    {
        var ids = command.Materials.Select(x => MaterialId.From(x.Id));
        var validation = await _materialValidatorService.ValidateMaterialsExistAsync(ids, ct);

        if (validation.IsFailure)
            return validation;

        var items = command.Materials.Select(item => item.ToValueObject()).ToList();
        var kit = Kit.Create(command.Name, command.Description, items);

        await _kitRepository.AddAsync(kit, ct);
        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
