using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Delete;

public class DeleteEquipmentHandler : ICommandHandler<DeleteEquipmentCommand, Result>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IAssetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteEquipmentHandler(
        IEquipmentRepository equipmentRepository,
        IAssetsUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async ValueTask<Result> Handle(DeleteEquipmentCommand command, CancellationToken ct)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(command.EquipmentId, ct);
        if (equipment is null || equipment.IsDeleted)
            return Result.Success();
        
        equipment.MarkAsRemoved(_currentUser.Id);
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}