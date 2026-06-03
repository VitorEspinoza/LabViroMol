using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Assets.Domain.Equipments;

namespace LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

public interface IMaintenanceRequestRepository
{
    Task AddAsync(MaintenanceRequest maintenanceRequest, CancellationToken cancellationToken);
    
    Task<List<MaintenanceRequest>> GetAllActiveByEquipmentIdAsync(Guid equipmentId, CancellationToken cancellationToken);
    
    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}