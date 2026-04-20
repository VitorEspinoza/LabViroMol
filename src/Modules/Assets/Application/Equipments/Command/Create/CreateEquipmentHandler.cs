using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Assets.Application.Equipments.Command.Create;

public class CreateEquipmentHandler : ICommandHandler<CreateEquipmentCommand, Result>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetsUnitOfWork _unitOfWork;

    public CreateEquipmentHandler(
        IEquipmentRepository equipmentRepository,
        ICurrentUser currentUser,
        IAssetsUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(CreateEquipmentCommand command, CancellationToken ct)
    {
        var existingCode = await _equipmentRepository.GetByCodeAsync(command.Code, ct);

        if (existingCode != null)
            return Result.BusinessRule("Código de equipamento já cadastrado.");

        var equipment = Equipment.Create(
            _currentUser.Id,
            command.Name,
            command.Brand,
            command.Model,
            command.Code,
            command.Description);

        await _equipmentRepository.AddAsync(equipment.Data!, ct);
        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}