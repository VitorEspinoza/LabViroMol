using LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
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
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<MaintenanceRequest> query = _context.MaintenanceRequests;

        query = query.WhereSearch(request.Search, x => x.Description, x => x.ProblemDescription);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "createdat" => request.SortDirection == "asc"
                ? query.OrderBy(req => EF.Property<DateTimeOffset>(req, "CreatedAt"))
                : query.OrderByDescending(req => EF.Property<DateTimeOffset>(req, "CreatedAt")),
            _ => query.OrderByDescending(req => EF.Property<DateTimeOffset>(req, "CreatedAt"))
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(req => new MaintenanceRequestViewModel(
                req.Id.Value,
                req.EquipmentId.Value,
                req.Description,
                req.ProblemDescription))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
