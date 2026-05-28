using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;

public record MaintenanceRequestViewModel(
    Guid Id,
    Guid EquipmentId,
    string Description,
    string ProblemDescription);