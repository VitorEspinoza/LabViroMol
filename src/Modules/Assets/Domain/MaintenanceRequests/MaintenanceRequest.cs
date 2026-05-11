using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

public class MaintenanceRequest : AggregateRoot<MaintenanceRequestId>
{
    private MaintenanceRequest() { }
    
    public MaintenanceRequestStatus Status { get; private set; }
    public string Description { get; private set; }
    public string ProblemDescription { get; private set; }
    public EquipmentId EquipmentId { get; private set; }

    private MaintenanceRequest(UserId createdBy, MaintenanceRequestId id, string description, string problemDescription,
        EquipmentId equipmentId) : base(id, createdBy)
    {
        Status = MaintenanceRequestStatus.Requested;
        Description = description;
        ProblemDescription = problemDescription;
        EquipmentId = equipmentId;
    }

    public static Result<MaintenanceRequest> Create(UserId createdBy,string description,
        string problemDescription, Guid equipmentId)
    {
        return new MaintenanceRequest(
            createdBy,
            IdFactory.New<MaintenanceRequestId>(),
            description: description,
            problemDescription: problemDescription,
            equipmentId: EquipmentId.From(equipmentId));
    }

    public Result Start(UserId updatedBy)
    {
        if (Status != MaintenanceRequestStatus.Requested)
            return Result.BusinessRule("Não é possível alterar o status para 'Em progresso'.");
        
        Status = MaintenanceRequestStatus.InProgress;
        MarkAsUpdated(updatedBy);
        return Result.Success();
    }

    public Result Done(UserId updatedBy)
    {
        if (Status != MaintenanceRequestStatus.InProgress)
            return Result.BusinessRule("Não é possível alterar o status para 'Finalizado'.");
        
        Status = MaintenanceRequestStatus.Done;
        MarkAsUpdated(updatedBy);
        return Result.Success();
    }

    public Result Cancel(UserId updatedBy)
    {
        if(Status == MaintenanceRequestStatus.Done)
            return Result.BusinessRule("Não é possível cancelar solicitações finalizadas.");
        
        Status = MaintenanceRequestStatus.Cancelled;
        MarkAsUpdated(updatedBy);
        return Result.Success();
    }
}