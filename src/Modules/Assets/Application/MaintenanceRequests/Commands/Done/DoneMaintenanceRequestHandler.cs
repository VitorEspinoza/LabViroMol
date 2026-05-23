using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Done;

public class DoneMaintenanceRequestHandler : ICommandHandler<DoneMaintenanceRequestCommand, Result>
{
    private readonly IMaintenanceRequestRepository _maintenanceRequestRepository;
    private readonly IAssetsUnitOfWork _unitOfWork;
    private ICurrentUser _currentUser;

    public DoneMaintenanceRequestHandler(
        IMaintenanceRequestRepository maintenanceRequestRepository,
        IAssetsUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _maintenanceRequestRepository = maintenanceRequestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }
    
    public async ValueTask<Result> Handle(DoneMaintenanceRequestCommand command, CancellationToken ct)
    {
        var maintenanceRequest = await _maintenanceRequestRepository.GetByIdAsync(command.MaintenanceRequestId, ct);
        if (maintenanceRequest is null)
            return Result.NotFound("Solicitação de manutenção não encontrada.");

        var result = maintenanceRequest.Done(_currentUser.Id);

        if (result.IsSuccess)
            await _unitOfWork.CompleteAsync(ct);
        
        return result;
    }
}