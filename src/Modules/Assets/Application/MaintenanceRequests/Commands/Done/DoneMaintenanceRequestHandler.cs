using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Done;

public class DoneMaintenanceRequestHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<DoneMaintenanceRequestCommand, Result>
{
    public async ValueTask<Result> Handle(DoneMaintenanceRequestCommand command, CancellationToken ct)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(command.MaintenanceRequestId, ct);
        if (maintenanceRequest is null)
            return Result.NotFound("Solicitação de manutenção não encontrada.");

        var result = maintenanceRequest.Done();

        if (result.IsSuccess)
            await unitOfWork.CompleteAsync(ct);

        return result;
    }
}
