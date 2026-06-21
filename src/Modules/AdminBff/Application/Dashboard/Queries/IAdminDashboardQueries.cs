using LabViroMol.Modules.AdminBff.Application.Dashboard.ViewModels;

namespace LabViroMol.Modules.AdminBff.Application.Dashboard.Queries;

public interface IAdminDashboardQueries
{
    Task<AdminDashboardSummaryViewModel> GetSummaryAsync(
        IReadOnlyCollection<string> permissions,
        CancellationToken ct);
}
