namespace LabViroMol.Modules.Inventory.Application.Reports;

public interface IStockReportQueries
{
    Task<StockOutflowsByProjectReport> GetStockOutflowsByProjectAsync(StockReportFilter filter, CancellationToken ct);
    Task<StockOutflowsByMonthReport> GetStockOutflowsByMonthAsync(StockReportFilter filter, CancellationToken ct);
    Task<StockOutflowTotalsReport> GetStockOutflowTotalsAsync(StockReportFilter filter, CancellationToken ct);
    Task<StockInflowsByOrderMaterialMonthReport> GetStockInflowsByOrderMaterialMonthAsync(StockReportFilter filter, CancellationToken ct);
    Task<CriticalStockBalanceReport> GetCriticalStockBalanceAsync(CriticalStockBalanceFilter filter, CancellationToken ct);
    Task<MaterialAuditMovementsReport> GetMaterialAuditMovementsAsync(MaterialAuditMovementsFilter filter, CancellationToken ct);
    Task<ManualStockAdjustmentsReport> GetManualStockAdjustmentsAsync(StockReportFilter filter, CancellationToken ct);
}
