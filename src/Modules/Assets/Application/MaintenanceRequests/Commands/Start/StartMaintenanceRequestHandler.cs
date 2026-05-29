using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Start;

public class StartMaintenanceRequestHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<StartMaintenanceRequestCommand, Result>
{
    public async ValueTask<Result> Handle(StartMaintenanceRequestCommand command, CancellationToken ct)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(command.MaintenanceRequestId, ct);
        if (maintenanceRequest is null)
            return Result.NotFound("Solicitação de manuteção não encontrada.");

        var result = maintenanceRequest.Start();

        if (result.IsSuccess)
            await unitOfWork.CompleteAsync(ct);

        return result;
    }
}
