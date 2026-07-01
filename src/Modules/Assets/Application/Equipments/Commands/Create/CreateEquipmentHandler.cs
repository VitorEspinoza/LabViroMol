using LabViroMol.Modules.Assets.Application.Equipments.EventHandlers;
using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.Equipments.Events;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;

public sealed class CreateEquipmentHandler : ICommandHandler<CreateEquipmentCommand, Result>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IAssetsUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;

    public CreateEquipmentHandler(
        IEquipmentRepository equipmentRepository,
        IAssetsUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<Result> Handle(CreateEquipmentCommand command, CancellationToken ct)
    {
        var existingCode = await _equipmentRepository.GetByCodeAsync(command.Code, ct);

        if (existingCode != null)
            return Result.BusinessRule("Código de equipamento já cadastrado.");

        var result = Equipment.Create(
            command.Name,
            command.Brand,
            command.Model,
            command.Code,
            command.Description,
            command.Location);

        if (result.IsFailure)
            return result;

        var equipment = result.Data!;

        await _equipmentRepository.AddAsync(equipment, ct);

        _unitOfWork.AddPersistentEvent(new EquipmentTranslationPersistentEvent());

        await _unitOfWork.CompleteAsync(ct);


        return Result.Success();
    }
}
