namespace LabViroMol.Modules.AdminBff.Application.Dashboard.ViewModels;

public record AdminDashboardUpcomingScheduleViewModel(
    Guid Id,
    string SchedulerName,
    DateOnly Date,
    DateTimeOffset StartDateHour,
    IReadOnlyList<string> EquipmentNames,
    string Status);
