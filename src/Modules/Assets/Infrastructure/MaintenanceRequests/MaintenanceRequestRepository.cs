using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.MaintenanceRequests;

public class MaintenanceRequestRepository : IMaintenanceRequestRepository
{
    private readonly AssetsDbContext _context;

    public MaintenanceRequestRepository(
        AssetsDbContext context)
    {
        _context = context;
    }
    
    public Task AddAsync(MaintenanceRequest maintenanceRequest, CancellationToken cancellationToken)
    {
        _context.MaintenanceRequests.Add(maintenanceRequest);
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<MaintenanceRequest>> GetAllActiveByEquipmentIdAsync(
        Guid equipmentId, 
        CancellationToken cancellationToken)
    {
        return await _context.MaintenanceRequests
            .Where(req => req.EquipmentId == equipmentId 
                          && (req.Status == MaintenanceRequestStatus.REQUESTED 
                              || req.Status == MaintenanceRequestStatus.ON_GOING))
            .ToListAsync(cancellationToken);
    }
}