using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.MaintenanceRequests;

internal sealed class MaintenanceRequestRepository : IMaintenanceRequestRepository
{
    private readonly AssetsDbContext _context;

    public MaintenanceRequestRepository(
        AssetsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MaintenanceRequest maintenanceRequest, CancellationToken cancellationToken)
    {
        await _context.MaintenanceRequests.AddAsync(maintenanceRequest, cancellationToken);
    }

    public async Task<List<MaintenanceRequest>> GetAllActiveByEquipmentIdAsync(
        Guid equipmentId,
        CancellationToken cancellationToken)
    {
        return await _context.MaintenanceRequests
            .Where(req => req.EquipmentId == equipmentId
                          && (req.Status == MaintenanceRequestStatus.Requested
                              || req.Status == MaintenanceRequestStatus.InProgress))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MaintenanceRequest>> GetAllByEquipmentIdAsync(
        Guid equipmentId,
        CancellationToken cancellationToken)
    {
        return await _context.MaintenanceRequests
            .Where(req => req.EquipmentId == equipmentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.MaintenanceRequests.FirstOrDefaultAsync(
            req => req.Id == id, cancellationToken);
    }

    public void Remove(MaintenanceRequest maintenanceRequest)
    {
        _context.MaintenanceRequests.Remove(maintenanceRequest);
    }
}
