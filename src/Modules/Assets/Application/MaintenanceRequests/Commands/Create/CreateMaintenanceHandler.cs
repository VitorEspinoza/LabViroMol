using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;

public class CreateMaintenanceHandler : ICommandHandler<CreateMaintenanceCommand, Result>
{
    private readonly IMaintenanceRequestRepository _maintenanceRequestRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetsUnitOfWork  _unitOfWork;
 
    public CreateMaintenanceHandler(
        IMaintenanceRequestRepository maintenanceRequestRepository,
        IEquipmentRepository equipmentRepository,
        ICurrentUser currentUser,
        IAssetsUnitOfWork unitOfWork)
    {
        _maintenanceRequestRepository = maintenanceRequestRepository;
        _equipmentRepository = equipmentRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask<Result> Handle(CreateMaintenanceCommand command, CancellationToken ct)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(command.EquipmentId, ct);
        if(equipment is null)
            return Result.NotFound("Equipamento não encontrado.");
        
        var openMaintenanceRequestForEquipment = await _maintenanceRequestRepository.GetAllActiveByEquipmentIdAsync(command.EquipmentId, ct);
        if (openMaintenanceRequestForEquipment.Count > 0)
            return Result.BusinessRule("Manutenção para equipamento já requisitada ou em andamento.");
        
        var maintenanceRequest = MaintenanceRequest.Create(
            _currentUser.Id,
            command.Description,
            command.ProblemDescription,
            command.EquipmentId);
        
        await _maintenanceRequestRepository.AddAsync(maintenanceRequest.Data!, ct);
        await _unitOfWork.CompleteAsync(ct);

        return maintenanceRequest;
    }
}