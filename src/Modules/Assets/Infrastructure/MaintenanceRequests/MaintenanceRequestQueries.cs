using LabViroMol.Modules.Assets.Application.MaintenanceRequests.Queries;
using LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.MaintenanceRequests;

internal sealed class MaintenanceRequestQueries : IMaintenanceRequestQueries
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

        var query = from req in _context.MaintenanceRequests
                    join equipment in _context.Equipments on req.EquipmentId equals equipment.Id
                    select new { Request = req, EquipmentName = equipment.Name };

        query = query.WhereSearch(request.Search, x => x.Request.Description, x => x.Request.ProblemDescription, x => x.EquipmentName);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "equipmentname" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.EquipmentName)
                : query.OrderBy(x => x.EquipmentName),
            "description" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Request.Description)
                : query.OrderBy(x => x.Request.Description),
            "createdat" => request.SortDirection == "asc"
                ? query.OrderBy(x => EF.Property<DateTimeOffset>(x.Request, "CreatedAt"))
                : query.OrderByDescending(x => EF.Property<DateTimeOffset>(x.Request, "CreatedAt")),
            _ => query.OrderByDescending(x => EF.Property<DateTimeOffset>(x.Request, "CreatedAt"))
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MaintenanceRequestViewModel(
                x.Request.Id.Value,
                x.Request.EquipmentId.Value,
                x.EquipmentName,
                x.Request.Description,
                x.Request.ProblemDescription,
                x.Request.Status.ToString(),
                EF.Property<DateTimeOffset>(x.Request, "CreatedAt"),
                EF.Property<DateTimeOffset?>(x.Request, "UpdatedAt")))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
