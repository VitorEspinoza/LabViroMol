using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Assets.Application.Equipments.Command.Update;

public class UpdateEquipmentHandler : ICommandHandler<UpdateEquipmentCommand, Result>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetsUnitOfWork _unitOfWork;

    public UpdateEquipmentHandler(
        IEquipmentRepository equipmentRepository,
        ICurrentUser currentUser,
        IAssetsUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }


    public async ValueTask<Result> Handle(UpdateEquipmentCommand command, CancellationToken ct)
    {
        var result = await _equipmentRepository.GetByIdAsync(command.EquipmentId, ct);

        if (result is null)
            return Result.BusinessRule("Equipamento não encontrado.");
        
        var conflictingCode = await _equipmentRepository.GetByCodeAsync(command.Code, ct);

        if (conflictingCode != null && conflictingCode.Id != command.EquipmentId)
            return Result.BusinessRule("Código de equipamento já registrado.");
        
        result.Update(
            command.Name,
            command.Brand,
            command.Model,
            command.Code,
            command.Description,
            _currentUser.Id);
        
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}