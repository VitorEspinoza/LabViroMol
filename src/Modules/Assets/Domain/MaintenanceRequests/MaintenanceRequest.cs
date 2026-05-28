using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

public class MaintenanceRequest : AggregateRoot<MaintenanceRequestId>, ICreationAuditable, IModificationAuditable
{
    private MaintenanceRequest() { }

    public MaintenanceRequestStatus Status { get; private set; }
    public string Description { get; private set; }
    public string ProblemDescription { get; private set; }
    public EquipmentId EquipmentId { get; private set; }

    private MaintenanceRequest(MaintenanceRequestId id, string description, string problemDescription,
        EquipmentId equipmentId) : base(id)
    {
        Status = MaintenanceRequestStatus.Requested;
        Description = description;
        ProblemDescription = problemDescription;
        EquipmentId = equipmentId;
    }

    public static Result<MaintenanceRequest> Create(string description,
        string problemDescription, Guid equipmentId)
    {
        return new MaintenanceRequest(
            IdFactory.New<MaintenanceRequestId>(),
            description: description,
            problemDescription: problemDescription,
            equipmentId: EquipmentId.From(equipmentId));
    }

    public Result Start()
    {
        if (Status != MaintenanceRequestStatus.Requested)
            return Result.BusinessRule("Não é possível alterar o status para 'Em progresso'.");

        Status = MaintenanceRequestStatus.InProgress;
        return Result.Success();
    }

    public Result Done()
    {
        if (Status != MaintenanceRequestStatus.InProgress)
            return Result.BusinessRule("Não é possível alterar o status para 'Finalizado'.");

        Status = MaintenanceRequestStatus.Done;
        return Result.Success();
    }

    public Result Cancel()
    {
        if(Status == MaintenanceRequestStatus.Done)
            return Result.BusinessRule("Não é possível cancelar solicitações finalizadas.");

        Status = MaintenanceRequestStatus.Cancelled;
        return Result.Success();
    }
}
