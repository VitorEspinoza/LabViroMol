using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Kits.Commands.Update;

public class UpdateKitHandler : ICommandHandler<UpdateKitCommand, Result>
{
    private readonly IKitRepository _kitRepository;
    private readonly MaterialValidatorService _materialValidatorService;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public UpdateKitHandler(
        IKitRepository kitRepository,
        MaterialValidatorService materialValidatorService,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork)
    {
        _kitRepository = kitRepository;
        _materialValidatorService = materialValidatorService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(UpdateKitCommand command, CancellationToken ct)
    {
        var kit = await _kitRepository.GetByIdAsync(command.KitId, ct);

        if (kit is null)
            return Result.NotFound("Kit não encontrado.");

        var ids = command.Materials.Select(x => MaterialId.From(x.Id));
        var validation = await _materialValidatorService.ValidateMaterialsExistAsync(ids, ct);

        if (validation.IsFailure)
            return validation;

        kit.UpdateMetadata(command.Name, command.Description, _currentUser.Id);
        kit.DefineMaterials(command.Materials.Select(item => item.ToValueObject()), _currentUser.Id);

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
