using LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
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

    public async Task<PagedResponse<MaintenanceRequestViewModel>> GetAllAsync(PagedRequest request)
    {
        var all = await _context.MaintenanceRequests
            .Select(req => new MaintenanceRequestViewModel(
                req.Id.Value,
                req.EquipmentId.Value,
                req.Description,
                req.ProblemDescription))
            .ToListAsync();

        return PagedResult.From(all, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<List<MaintenanceRequestViewModel>> GetAllMaintenanceRequestsAsync()
    {
        return await _context.MaintenanceRequests
            .Select(req => new MaintenanceRequestViewModel(
                req.Id.Value,
                req.EquipmentId.Value,
                req.Description,
                req.ProblemDescription))
            .ToListAsync();
    }
}
