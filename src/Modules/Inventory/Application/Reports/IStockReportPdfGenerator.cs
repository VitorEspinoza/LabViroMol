namespace LabViroMol.Modules.Inventory.Application.Reports;

public interface IStockReportPdfGenerator
{
    byte[] GenerateStockOutflowsByProject(StockOutflowsByProjectReport report);
    byte[] GenerateStockOutflowsByMonth(StockOutflowsByMonthReport report);
    byte[] GenerateStockOutflowTotals(StockOutflowTotalsReport report);
    byte[] GenerateStockInflowsByOrderMaterialMonth(StockInflowsByOrderMaterialMonthReport report);
    byte[] GenerateCriticalStockBalance(CriticalStockBalanceReport report);
    byte[] GenerateMaterialAuditMovements(MaterialAuditMovementsReport report);
    byte[] GenerateManualStockAdjustments(ManualStockAdjustmentsReport report);
    byte[] GenerateStockMovementsByUser(StockMovementsByUserReport report);
    byte[] GenerateIdleStock(IdleStockReport report);
    byte[] GenerateOrderStatusCycle(OrderStatusCycleReport report);
    byte[] GenerateStockByMaterialType(StockByMaterialTypeReport report);
}
