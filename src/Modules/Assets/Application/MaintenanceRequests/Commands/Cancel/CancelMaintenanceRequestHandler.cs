using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Cancel;

public sealed class CancelMaintenanceRequestHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IAssetsUnitOfWork assetsUnitOfWork) : ICommandHandler<CancelMaintenanceRequestCommand, Result>
{
    public async ValueTask<Result> Handle(CancelMaintenanceRequestCommand command, CancellationToken ct)
    {
        var request = await maintenanceRequestRepository.GetByIdAsync(command.MaintenanceRequestId, ct);
        if (request is null)
            return Result.NotFound("Solicitação de manutenção não encontrada.");

        var result = request.Cancel();
        if (result.IsSuccess)
            await assetsUnitOfWork.CompleteAsync(ct);

        return result;
    }
}
