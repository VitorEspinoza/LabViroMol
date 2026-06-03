using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;

public class CreateEquipmentHandler(
    IEquipmentRepository equipmentRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<CreateEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(CreateEquipmentCommand command, CancellationToken ct)
    {
        var existingCode = await equipmentRepository.GetByCodeAsync(command.Code, ct);

        if (existingCode != null)
            return Result.BusinessRule("Código de equipamento já cadastrado.");

        var equipment = Equipment.Create(
            command.Name,
            command.Brand,
            command.Model,
            command.Code,
            command.Description);

        await equipmentRepository.AddAsync(equipment.Data!, ct);
        await unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
