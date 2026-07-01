using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;

public sealed class CreateMaintenanceHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IEquipmentRepository equipmentRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<CreateMaintenanceCommand, Result>
{
    public async ValueTask<Result> Handle(CreateMaintenanceCommand command, CancellationToken ct)
    {
        var equipment = await equipmentRepository.GetByIdAsync(command.EquipmentId, ct);
        if (equipment is null)
            return Result.NotFound("Equipamento não encontrado.");

        var openMaintenanceRequestForEquipment = await maintenanceRequestRepository.GetAllActiveByEquipmentIdAsync(command.EquipmentId, ct);
        if (openMaintenanceRequestForEquipment.Count > 0)
            return Result.BusinessRule("Manutenção para equipamento já requisitada ou em andamento.");

        var maintenanceRequest = MaintenanceRequest.Create(
            command.Description,
            command.ProblemDescription,
            command.EquipmentId);

        await maintenanceRequestRepository.AddAsync(maintenanceRequest.Data!, ct);
        await unitOfWork.CompleteAsync(ct);

        return maintenanceRequest;
    }
}
