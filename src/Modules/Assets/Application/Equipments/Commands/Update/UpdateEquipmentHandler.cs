using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;

public class UpdateEquipmentHandler(
    IEquipmentRepository equipmentRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<UpdateEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateEquipmentCommand command, CancellationToken ct)
    {
        var result = await equipmentRepository.GetByIdAsync(command.EquipmentId, ct);

        if (result is null)
            return Result.BusinessRule("Equipamento não encontrado.");

        var conflictingCode = await equipmentRepository.GetByCodeAsync(command.Code, ct);

        if (conflictingCode != null && conflictingCode.Id != command.EquipmentId)
            return Result.BusinessRule("Código de equipamento já registrado.");

        result.Update(
            command.Name,
            command.Brand,
            command.Model,
            command.Code,
            command.Description,
            command.Location);

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
