namespace LabViroMol.Modules.Inventory.Application.Reports;

public abstract record StockReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId);

public sealed record StockOutflowsByProjectReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    Guid? ProjectId,
    IReadOnlyList<StockOutflowsByProjectRow> Rows)
    : StockReport(GeneratedAtUtc, From, To, MaterialId, MaterialTypeId);

public sealed record StockOutflowsByProjectRow(
    Guid ProjectId,
    string ProjectTitle,
    Guid MaterialId,
    string MaterialName,
    string Unit,
    decimal TotalQuantity,
    int MovementsCount,
    DateTime FirstMovementAt,
    DateTime LastMovementAt);

public sealed record StockOutflowsByMonthReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    IReadOnlyList<StockOutflowsByMonthRow> Rows)
    : StockReport(GeneratedAtUtc, From, To, MaterialId, MaterialTypeId);

public sealed record StockOutflowsByMonthRow(
    int Year,
    int Month,
    Guid MaterialId,
    string MaterialName,
    string Unit,
    string OutflowType,
    decimal TotalQuantity,
    int MovementsCount);

public sealed record StockOutflowTotalsReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    IReadOnlyList<StockOutflowTotalsRow> Rows)
    : StockReport(GeneratedAtUtc, From, To, MaterialId, MaterialTypeId);

public sealed record StockOutflowTotalsRow(
    Guid MaterialId,
    string MaterialName,
    string MaterialTypeName,
    string Unit,
    decimal ProjectConsumptionQuantity,
    decimal ExceptionOutQuantity,
    decimal TotalQuantity,
    decimal ParticipationPercent);

public sealed record StockInflowsByOrderMaterialMonthReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    IReadOnlyList<StockInflowsByOrderMaterialMonthRow> Rows)
    : StockReport(GeneratedAtUtc, From, To, MaterialId, MaterialTypeId);

public sealed record StockInflowsByOrderMaterialMonthRow(
    int Year,
    int Month,
    Guid? OrderId,
    Guid MaterialId,
    string MaterialName,
    string Unit,
    string InflowType,
    decimal TotalQuantity,
    int MovementsCount);

public sealed record CriticalStockBalanceReport(
    DateTime GeneratedAtUtc,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    bool OnlyCritical,
    IReadOnlyList<CriticalStockBalanceRow> Rows);

public sealed record CriticalStockBalanceRow(
    Guid MaterialId,
    string MaterialName,
    string MaterialTypeName,
    string Location,
    string Unit,
    decimal StockQuantity,
    decimal MinStock,
    decimal Difference);

public sealed record MaterialAuditMovementsReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    string? TransactionType,
    int Limit,
    IReadOnlyList<MaterialAuditMovementRow> Rows);

public sealed record MaterialAuditMovementRow(
    Guid TransactionId,
    Guid MaterialId,
    string MaterialName,
    string MaterialTypeName,
    string Unit,
    string TransactionType,
    decimal Quantity,
    DateTime TransactedAt,
    Guid TransactedByUserId,
    string TransactedByUserName,
    Guid? ProjectId,
    string? ProjectTitle,
    Guid? OrderId,
    string? Justification);

public sealed record ManualStockAdjustmentsReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    IReadOnlyList<ManualStockAdjustmentRow> Rows)
    : StockReport(GeneratedAtUtc, From, To, MaterialId, MaterialTypeId);

public sealed record ManualStockAdjustmentRow(
    Guid TransactionId,
    Guid MaterialId,
    string MaterialName,
    string MaterialTypeName,
    string Unit,
    string AdjustmentType,
    decimal Quantity,
    DateTime TransactedAt,
    Guid TransactedByUserId,
    string TransactedByUserName,
    string? Justification);

public sealed record StockMovementsByUserReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    string? TransactionType,
    IReadOnlyList<StockMovementsByUserRow> Rows);

public sealed record StockMovementsByUserRow(
    Guid UserId,
    string UserName,
    string TransactionType,
    decimal TotalQuantity,
    int MovementsCount);

public sealed record IdleStockReport(
    DateTime GeneratedAtUtc,
    Guid? MaterialTypeId,
    DateTime Since,
    IReadOnlyList<IdleStockRow> Rows);

public sealed record IdleStockRow(
    Guid MaterialId,
    string MaterialName,
    string MaterialTypeName,
    string Location,
    string Unit,
    decimal StockQuantity,
    DateTime? LastMovementAt);

public sealed record OrderStatusCycleReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    int StaleDays,
    IReadOnlyList<OrderStatusCountRow> StatusCounts,
    double? AveragePendingToProcessingHours,
    double? AverageProcessingToCompletedHours,
    IReadOnlyList<StaleOrderRow> StaleOrders);

public sealed record OrderStatusCountRow(string Status, int Count);

public sealed record StaleOrderRow(
    Guid OrderId,
    string MaterialName,
    string Status,
    DateTime LastTransitionAt,
    int DaysInStatus);

public sealed record StockByMaterialTypeReport(
    DateTime GeneratedAtUtc,
    DateTime? From,
    DateTime? To,
    IReadOnlyList<StockByMaterialTypeRow> Rows);

public sealed record StockByMaterialTypeRow(
    Guid MaterialTypeId,
    string MaterialTypeName,
    decimal InflowQuantity,
    decimal OutflowQuantity,
    decimal NetQuantity,
    decimal CurrentStockQuantity,
    int MaterialsCount);
