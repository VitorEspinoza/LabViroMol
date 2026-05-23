using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Cancel;

public class CancelMaintenanceRequestHandler : ICommandHandler<CancelMaintenanceRequestCommand, Result>
{
    private readonly IMaintenanceRequestRepository _maintenanceRequestRepository;
    private readonly IAssetsUnitOfWork _assetsUnitOfWork;
    private readonly ICurrentUser _currentUser;

    public CancelMaintenanceRequestHandler(
        IMaintenanceRequestRepository maintenanceRequestRepository,
        IAssetsUnitOfWork assetsUnitOfWork,
        ICurrentUser currentUser)
    {
        _maintenanceRequestRepository = maintenanceRequestRepository;
        _assetsUnitOfWork = assetsUnitOfWork;
        _currentUser = currentUser;
    }
    
    public async ValueTask<Result> Handle(CancelMaintenanceRequestCommand command, CancellationToken ct)
    {
        var request = await _maintenanceRequestRepository.GetByIdAsync(command.MaintenanceRequestId, ct);
        if (request is null)
            return Result.NotFound("Solicitação de manutenção não encontrada.");

        var result = request.Cancel(_currentUser.Id);
        if (result.IsSuccess)
            await _assetsUnitOfWork.CompleteAsync(ct);
        
        return result;
    }
}