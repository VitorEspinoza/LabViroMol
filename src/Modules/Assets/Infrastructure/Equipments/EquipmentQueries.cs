using LabViroMol.Modules.Assets.Application.Equipments.ViewModels;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.Equipments;

public class EquipmentQueries
{
    private readonly AssetsDbContext _context;

    public EquipmentQueries(AssetsDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<PagedResponse<EquipmentViewModel>> GetAllInstitutionalAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<EquipmentViewModel> query = _context.Equipments
            .Select(e => new EquipmentViewModel(
                e.Id.Value,
                e.Name,
                e.Model,
                e.Brand,
                e.Code,
                e.Description,
                e.ImageUrl));

        query = query.WhereSearch(request.Search,
            x => x.Name, x => x.Model, x => x.Brand, x => x.Code, x => x.Description);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Code)
                : query.OrderBy(e => e.Code),
            "brand" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Brand)
                : query.OrderBy(e => e.Brand),
            "model" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Model)
                : query.OrderBy(e => e.Model),
            _ => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Name)
                : query.OrderBy(e => e.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<EquipmentAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<EquipmentAdminSummaryViewModel> query = _context.Equipments
            .Select(e => new EquipmentAdminSummaryViewModel(
                e.Id.Value,
                e.Code,
                e.Name,
                e.Model,
                e.Brand,
                e.Location,
                _context.MaintenanceRequests.Any(mr => mr.EquipmentId == e.Id && mr.Status == MaintenanceRequestStatus.InProgress)
                    ? "UnderMaintenance"
                    : _context.MaintenanceRequests.Any(mr => mr.EquipmentId == e.Id && mr.Status == MaintenanceRequestStatus.Requested)
                        ? "MaintenancePending"
                        : "Available"));

        query = query.WhereSearch(request.Search,
            x => x.Code, x => x.Name, x => x.Model, x => x.Brand, x => x.Location, x => x.Status);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Code)
                : query.OrderBy(e => e.Code),
            "brand" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Brand)
                : query.OrderBy(e => e.Brand),
            "model" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Model)
                : query.OrderBy(e => e.Model),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Status)
                : query.OrderBy(e => e.Status),
            _ => request.SortDirection == "desc"
                ? query.OrderByDescending(e => e.Name)
                : query.OrderBy(e => e.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<EquipmentAdminDetailViewModel?> GetAdminByIdAsync(Guid id)
    {
        return await _context.Equipments
            .Where(e => e.Id == id)
            .Select(e => new EquipmentAdminDetailViewModel(
                e.Id.Value,
                e.Name,
                e.Model,
                e.Brand,
                e.Code,
                e.Location,
                e.Description,
                e.ImageUrl))
            .FirstOrDefaultAsync();
    }

    public async Task<EquipmentViewModel?> GetEquipmentById(Guid id)
    {
        return await _context.Equipments
            .Where(e => e.Id == id)
            .Select(e => new EquipmentViewModel(
                e.Id!.Value,
                e.Name,
                e.Model,
                e.Brand,
                e.Code,
                e.Description,
                e.ImageUrl))
            .FirstOrDefaultAsync();
    }
}
