using LabViroMol.Modules.AdminBff.Application.Dashboard.Queries;
using LabViroMol.Modules.AdminBff.Application.Dashboard.ViewModels;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.AdminBff.Infrastructure.Dashboard;

public class AdminDashboardQueries : IAdminDashboardQueries
{
    private const int PreviewSize = 5;

    private readonly SchedulingDbContext _schedulingContext;
    private readonly InventoryDbContext _inventoryContext;
    private readonly AssetsDbContext _assetsContext;

    public AdminDashboardQueries(
        SchedulingDbContext schedulingContext,
        InventoryDbContext inventoryContext,
        AssetsDbContext assetsContext)
    {
        _schedulingContext = schedulingContext;
        _inventoryContext = inventoryContext;
        _assetsContext = assetsContext;
    }

    public async Task<AdminDashboardSummaryViewModel> GetSummaryAsync(
        IReadOnlyCollection<string> permissions,
        CancellationToken ct)
    {
        var permissionSet = permissions.ToHashSet(StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;

        var scheduling = HasViewPermission(permissionSet, Permissions.Scheduling.SchedulesView)
            ? await GetSchedulingSummaryAsync(now, ct)
            : null;

        var inventory = HasViewPermission(permissionSet, Permissions.Inventory.MaterialsView)
            ? await GetInventorySummaryAsync(ct)
            : null;

        var assets = HasViewPermission(permissionSet, Permissions.Assets.MaintenanceView)
            ? await GetAssetsSummaryAsync(ct)
            : null;

        return new AdminDashboardSummaryViewModel(
            scheduling,
            inventory,
            assets,
            now);
    }

    private async Task<AdminDashboardSchedulingSummaryViewModel> GetSchedulingSummaryAsync(
        DateTimeOffset now,
        CancellationToken ct)
    {
        var currentMonth = DateOnly.FromDateTime(now.UtcDateTime);
        var monthStart = new DateOnly(currentMonth.Year, currentMonth.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var pendingCount = await _schedulingContext.Schedules
            .AsNoTracking()
            .CountAsync(s =>
                s.Status == ScheduleStatus.PENDING &&
                s.Scheduling.StartDateHour > now,
                ct);

        var approvedThisMonthCount = await _schedulingContext.Schedules
            .AsNoTracking()
            .CountAsync(s =>
                s.Status == ScheduleStatus.SCHEDULED &&
                s.Scheduling.Date >= monthStart &&
                s.Scheduling.Date < nextMonthStart,
                ct);

        var upcomingSchedules = await _schedulingContext.Schedules
            .AsNoTracking()
            .Where(s =>
                s.Status == ScheduleStatus.SCHEDULED &&
                s.Scheduling.StartDateHour >= now)
            .OrderBy(s => s.Scheduling.StartDateHour)
            .Take(PreviewSize)
            .Select(s => new
            {
                Id = s.Id.Value,
                s.Scheduler.Name,
                s.Scheduling.Date,
                s.Scheduling.StartDateHour,
                EquipmentNames = s.Equipments.Select(e => e.Name).ToList(),
                s.Status
            })
            .ToListAsync(ct);

        var upcoming = upcomingSchedules
            .Select(s => new AdminDashboardUpcomingScheduleViewModel(
                s.Id,
                s.Name,
                s.Date,
                s.StartDateHour,
                s.EquipmentNames,
                s.Status.ToString()))
            .ToList();

        return new AdminDashboardSchedulingSummaryViewModel(
            pendingCount,
            approvedThisMonthCount,
            upcoming);
    }

    private async Task<AdminDashboardInventorySummaryViewModel> GetInventorySummaryAsync(CancellationToken ct)
    {
        var lowStockQuery = _inventoryContext.Materials
            .AsNoTracking()
            .Where(m =>
                EF.Property<decimal>(m, nameof(m.StockQuantity)) <=
                EF.Property<decimal>(m, nameof(m.MinStock)));

        var lowStockCount = await lowStockQuery.CountAsync(ct);

        var lowStockMaterials = await lowStockQuery
            .OrderBy(m => EF.Property<decimal>(m, nameof(m.StockQuantity)))
            .ThenBy(m => m.Name)
            .Take(PreviewSize)
            .Select(m => new AdminDashboardMaterialAlertViewModel(
                m.Id.Value,
                m.Name,
                m.Location,
                EF.Property<decimal>(m, nameof(m.StockQuantity)),
                EF.Property<decimal>(m, nameof(m.MinStock)),
                m.Unit.ToString()))
            .ToListAsync(ct);

        return new AdminDashboardInventorySummaryViewModel(
            lowStockCount,
            lowStockMaterials);
    }

    private async Task<AdminDashboardAssetsSummaryViewModel> GetAssetsSummaryAsync(CancellationToken ct)
    {
        var activeMaintenanceCount = await _assetsContext.MaintenanceRequests
            .AsNoTracking()
            .CountAsync(m =>
                m.Status == MaintenanceRequestStatus.Requested ||
                m.Status == MaintenanceRequestStatus.InProgress,
                ct);

        return new AdminDashboardAssetsSummaryViewModel(activeMaintenanceCount);
    }

    private static bool HasViewPermission(IReadOnlySet<string> permissions, string viewPermission)
    {
        var managePermission = viewPermission.Replace($".{Permissions.View}", $".{Permissions.Manage}");

        return permissions.Contains(viewPermission) ||
               permissions.Contains(managePermission);
    }
}
