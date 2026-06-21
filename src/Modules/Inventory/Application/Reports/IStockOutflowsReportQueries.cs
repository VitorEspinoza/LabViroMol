using LabViroMol.Modules.Inventory.Application.Reports.ViewModels;

namespace LabViroMol.Modules.Inventory.Application.Reports;

public interface IStockOutflowsReportQueries
{
    Task<IReadOnlyList<StockOutflowsByProjectViewModel>> GetByProjectAsync(
        StockOutflowsReportFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyList<StockOutflowsByMonthViewModel>> GetByMonthAsync(
        StockOutflowsReportFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyList<StockOutflowsTotalsViewModel>> GetTotalsAsync(
        StockOutflowsReportFilter filter,
        CancellationToken ct = default);
}
