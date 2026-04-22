using LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.MaintenanceRequests;

public class MaintenanceRequestQueries
{
    private readonly AssetsDbContext _context;
    
    public MaintenanceRequestQueries(AssetsDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<List<MaintenanceRequestViewModel>> GetAllMaintenanceRequestsAsync()
    {
        var maintRequest = await _context.MaintenanceRequests
            .Select(req => new MaintenanceRequestViewModel(
                req.Id.Value,
                req.EquipmentId.Value,
                req.Description,
                req.ProblemDescription))
            .ToListAsync();
        
        return maintRequest;
    }
}