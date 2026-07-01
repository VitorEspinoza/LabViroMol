using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Delete;

public sealed class DeleteEquipmentHandler(
    IEquipmentRepository equipmentRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<DeleteEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteEquipmentCommand command, CancellationToken ct)
    {
        var equipment = await equipmentRepository.GetByIdAsync(command.EquipmentId, ct);
        if (equipment is null)
            return Result.Success();

        equipment.Delete();
        equipmentRepository.Remove(equipment);
        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
