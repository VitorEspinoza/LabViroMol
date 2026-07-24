using LabViroMol.Modules.Assets.Application.Equipments.Queries;
using LabViroMol.Modules.Assets.Application.Equipments.ViewModels;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.Equipments;

internal sealed class EquipmentQueries : IEquipmentQueries
{
    private readonly AssetsDbContext _context;

    public EquipmentQueries(AssetsDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<PagedResponse<EquipmentViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Equipment> query = _context.Equipments;

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

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(e => new EquipmentViewModel(
                e.Id.Value,
                e.GetName(language),
                e.Model,
                e.Brand,
                e.Code,
                e.GetDescription(language),
                e.ImageUrl))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<EquipmentAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var query = _context.Equipments.Select(e => new
        {
            Equipment = e,
            Status = _context.MaintenanceRequests.Any(mr => mr.EquipmentId == e.Id && mr.Status == MaintenanceRequestStatus.InProgress)
                ? "UnderMaintenance"
                : _context.MaintenanceRequests.Any(mr => mr.EquipmentId == e.Id && mr.Status == MaintenanceRequestStatus.Requested)
                    ? "MaintenancePending"
                    : "Available"
        });

        query = query.WhereSearch(request.Search,
            x => x.Equipment.Code, x => x.Equipment.Name, x => x.Equipment.Model,
            x => x.Equipment.Brand, x => x.Equipment.Location, x => x.Status);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Equipment.Code)
                : query.OrderBy(x => x.Equipment.Code),
            "brand" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Equipment.Brand)
                : query.OrderBy(x => x.Equipment.Brand),
            "model" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Equipment.Model)
                : query.OrderBy(x => x.Equipment.Model),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),
            _ => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Equipment.Name)
                : query.OrderBy(x => x.Equipment.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new EquipmentAdminSummaryViewModel(
                x.Equipment.Id.Value,
                x.Equipment.Code,
                x.Equipment.Name,
                x.Equipment.Model,
                x.Equipment.Brand,
                x.Equipment.Location,
                x.Equipment.Description,
                x.Status,
                x.Equipment.ImageUrl))
            .ToListAsync();

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

    public async Task<EquipmentViewModel?> GetEquipmentByIdInstitutional(Guid id, string? language)
    {
        return await _context.Equipments
            .Where(e => e.Id == id)
            .Select(e => new EquipmentViewModel(
                e.Id!.Value,
                e.GetName(language),
                e.Model,
                e.Brand,
                e.Code,
                e.GetDescription(language),
                e.ImageUrl))
            .FirstOrDefaultAsync();
    }

    public async Task<List<EquipmentSchedulableViewModel>> GetSchedulableEquipments(string? language)
    {
        return await _context.Equipments
            .Where(e => !_context.MaintenanceRequests
                .Where(mr => mr.Status == MaintenanceRequestStatus.InProgress
                             || mr.Status == MaintenanceRequestStatus.Requested)
                .Select(mr => mr.EquipmentId)
                .Contains(e.Id))
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentSchedulableViewModel(
                e.Id.Value,
                e.GetName(language)
                ))
            .ToListAsync();
    }
}
