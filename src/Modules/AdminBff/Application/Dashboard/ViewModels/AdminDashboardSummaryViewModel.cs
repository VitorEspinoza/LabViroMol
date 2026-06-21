namespace LabViroMol.Modules.AdminBff.Application.Dashboard.ViewModels;

public record AdminDashboardSummaryViewModel(
    AdminDashboardSchedulingSummaryViewModel? Scheduling,
    AdminDashboardInventorySummaryViewModel? Inventory,
    AdminDashboardAssetsSummaryViewModel? Assets,
    DateTimeOffset GeneratedAt);

public record AdminDashboardSchedulingSummaryViewModel(
    int PendingSchedulesCount,
    int ApprovedSchedulesThisMonthCount,
    IReadOnlyList<AdminDashboardUpcomingScheduleViewModel> UpcomingSchedules);

public record AdminDashboardInventorySummaryViewModel(
    int LowStockMaterialsCount,
    IReadOnlyList<AdminDashboardMaterialAlertViewModel> LowStockMaterials);

public record AdminDashboardAssetsSummaryViewModel(
    int ActiveMaintenanceRequestsCount);
