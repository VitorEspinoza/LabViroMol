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
        var all = await _context.Equipments
            .Select(e => new EquipmentViewModel(
                e.Id.Value,
                e.Name,
                e.Model,
                e.Brand,
                e.Code,
                e.Description,
                e.ImageUrl))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Code).ToList()
                : all.OrderBy(e => e.Code).ToList(),
            "brand" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Brand).ToList()
                : all.OrderBy(e => e.Brand).ToList(),
            "model" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Model).ToList()
                : all.OrderBy(e => e.Model).ToList(),
            _ => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Name).ToList()
                : all.OrderBy(e => e.Name).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<PagedResponse<EquipmentAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var equipments = await _context.Equipments
            .Select(e => new
            {
                e.Id,
                e.Code,
                e.Name,
                e.Model,
                e.Brand,
                e.Location,
                HasInProgress = _context.MaintenanceRequests
                    .Any(mr => mr.EquipmentId == e.Id && mr.Status == MaintenanceRequestStatus.InProgress),
                HasRequested = _context.MaintenanceRequests
                    .Any(mr => mr.EquipmentId == e.Id && mr.Status == MaintenanceRequestStatus.Requested)
            })
            .ToListAsync();

        var all = equipments.Select(e => new EquipmentAdminSummaryViewModel(
            e.Id.Value,
            e.Code,
            e.Name,
            e.Model,
            e.Brand,
            e.Location,
            e.HasInProgress ? "UnderMaintenance"
                : e.HasRequested ? "MaintenancePending"
                : "Available")).ToList();

        var sorted = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Code).ToList()
                : all.OrderBy(e => e.Code).ToList(),
            "brand" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Brand).ToList()
                : all.OrderBy(e => e.Brand).ToList(),
            "model" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Model).ToList()
                : all.OrderBy(e => e.Model).ToList(),
            "status" => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Status).ToList()
                : all.OrderBy(e => e.Status).ToList(),
            _ => request.SortDirection == "desc"
                ? all.OrderByDescending(e => e.Name).ToList()
                : all.OrderBy(e => e.Name).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
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

    public async Task<List<EquipmentViewModel>> GetAllEquipments()
    {
        return await _context.Equipments
            .Select(e => new EquipmentViewModel(
                e.Id.Value,
                e.Name,
                e.Model,
                e.Brand,
                e.Code,
                e.Description,
                e.ImageUrl))
            .ToListAsync();
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
