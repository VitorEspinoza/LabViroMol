using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

public class MaintenanceRequest : AggregateRoot<MaintenanceRequestId>
{
    private MaintenanceRequest() { }
    
    public MaintenanceRequestStatus Status { get; private set; }
    public string Description { get; private set; }
    public string ProblemDescription { get; private set; }
    public EquipmentId EquipmentId { get; private set; }

    private MaintenanceRequest(MaintenanceRequestStatus status, string description, string problemDescription,
        EquipmentId equipmentId)
    {
        Status = status;
        Description = description;
        ProblemDescription = problemDescription;
        EquipmentId = equipmentId;
    }

    public static Result<MaintenanceRequest> Create(MaintenanceRequestStatus status, string description,
        string problemDescription, Guid equipmentId)
    {
        return new MaintenanceRequest(
            status: status,
            description: description,
            problemDescription: problemDescription,
            equipmentId: EquipmentId.From(equipmentId));
    }
}