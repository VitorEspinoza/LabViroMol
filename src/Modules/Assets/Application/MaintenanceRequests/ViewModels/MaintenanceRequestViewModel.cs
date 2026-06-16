using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;

public record MaintenanceRequestViewModel(
    Guid MaintenanceRequestId,
    Guid EquipmentId,
    string EquipmentName,
    string Description,
    string ProblemDescription,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);