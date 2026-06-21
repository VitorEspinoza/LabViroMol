using LabViroMol.Modules.Inventory.Domain.Materials;

namespace LabViroMol.Modules.Inventory.Application.Reports;

public interface IStockEntryBalanceAuditReportQueries
{
    Task<IReadOnlyCollection<StockInflowByOrderMaterialMonthReportItem>> GetStockInflowsByOrderMaterialMonthAsync(
        StockInflowByOrderMaterialMonthReportFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyCollection<CriticalStockBalanceReportItem>> GetCriticalStockBalanceAsync(
        CriticalStockBalanceReportFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyCollection<MaterialAuditMovementReportItem>> GetMaterialAuditMovementsAsync(
        MaterialAuditMovementsReportFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyCollection<ManualStockAdjustmentReportItem>> GetManualStockAdjustmentsAsync(
        ManualStockAdjustmentsReportFilter filter,
        CancellationToken ct = default);
}

public sealed record StockInflowByOrderMaterialMonthReportFilter(
    DateTime? From = null,
    DateTime? To = null,
    Guid? MaterialId = null,
    Guid? MaterialTypeId = null);

public sealed record CriticalStockBalanceReportFilter(
    Guid? MaterialId = null,
    Guid? MaterialTypeId = null,
    bool OnlyCritical = true);

public sealed record MaterialAuditMovementsReportFilter(
    DateTime? From = null,
    DateTime? To = null,
    Guid? MaterialId = null,
    Guid? MaterialTypeId = null,
    TransactionType? TransactionType = null,
    int? MaxRows = null);

public sealed record ManualStockAdjustmentsReportFilter(
    DateTime? From = null,
    DateTime? To = null,
    Guid? MaterialId = null,
    Guid? MaterialTypeId = null,
    int? MaxRows = null);

public sealed record StockInflowByOrderMaterialMonthReportItem(
    int Year,
    int Month,
    Guid MaterialId,
    string MaterialName,
    Guid MaterialTypeId,
    string MaterialTypeName,
    string Unit,
    string EntryType,
    Guid? OrderId,
    int TransactionCount,
    decimal TotalQuantity);

public sealed record CriticalStockBalanceReportItem(
    Guid MaterialId,
    string MaterialName,
    Guid MaterialTypeId,
    string MaterialTypeName,
    string Location,
    string Unit,
    decimal StockQuantity,
    decimal MinStock,
    decimal Difference);

public sealed record MaterialAuditMovementReportItem(
    Guid TransactionId,
    Guid MaterialId,
    string MaterialName,
    Guid MaterialTypeId,
    string MaterialTypeName,
    string Unit,
    string TransactionType,
    decimal Quantity,
    DateTime TransactedAt,
    Guid TransactedByUserId,
    string? Justification,
    Guid? OrderId,
    Guid? ProjectId);

public sealed record ManualStockAdjustmentReportItem(
    Guid TransactionId,
    Guid MaterialId,
    string MaterialName,
    Guid MaterialTypeId,
    string MaterialTypeName,
    string Unit,
    string AdjustmentType,
    decimal Quantity,
    DateTime TransactedAt,
    Guid TransactedByUserId,
    string Justification);
