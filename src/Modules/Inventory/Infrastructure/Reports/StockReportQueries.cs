using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Reports;

internal sealed class StockReportQueries(
    InventoryDbContext context,
    IProjectCatalog projectCatalog,
    IUserCatalog userCatalog) : IStockReportQueries
{
    private const string RemovedUserFallback = "Usuario removido";
    private const string RemovedProjectFallback = "Projeto removido";

    public async Task<StockOutflowsByProjectReport> GetStockOutflowsByProjectAsync(
        StockReportFilter filter,
        CancellationToken ct)
    {
        var query = ApplyCommonTransactionFilters(
            context.StockTransactions.AsNoTracking()
                .Where(t => t.Type == TransactionType.ProjectConsumption && t.ProjectId.HasValue),
            filter);

        if (filter.ProjectId.HasValue)
        {
            var projectId = ProjectId.From(filter.ProjectId.Value);
            query = query.Where(t => t.ProjectId == projectId);
        }

        var entries = await query
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, Material = m })
            .ToListAsync(ct);

        var rows = entries
            .GroupBy(x => new
            {
                ProjectId = x.Transaction.ProjectId!.Value.Value,
                MaterialId = x.Material.Id.Value,
                x.Material.Name,
                x.Material.Unit
            })
            .Select(g => new
            {
                g.Key.ProjectId,
                g.Key.MaterialId,
                MaterialName = g.Key.Name,
                Unit = g.Key.Unit.ToString(),
                TotalQuantity = g.Sum(x => x.Transaction.Quantity.Value),
                MovementsCount = g.Count(),
                FirstMovementAt = g.Min(x => x.Transaction.TransactedAt),
                LastMovementAt = g.Max(x => x.Transaction.TransactedAt)
            })
            .OrderBy(x => x.MaterialName)
            .ThenBy(x => x.ProjectId)
            .ToList();

        var projectTitles = await projectCatalog.GetProjectTitlesAsync(rows.Select(r => r.ProjectId), ct);

        return new StockOutflowsByProjectReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
            filter.ProjectId,
            rows.Select(r => new StockOutflowsByProjectRow(
                    r.ProjectId,
                    projectTitles.GetValueOrDefault(r.ProjectId, $"Projeto {r.ProjectId}"),
                    r.MaterialId,
                    r.MaterialName,
                    r.Unit,
                    r.TotalQuantity,
                    r.MovementsCount,
                    r.FirstMovementAt,
                    r.LastMovementAt))
                .ToList());
    }

    public async Task<StockOutflowsByMonthReport> GetStockOutflowsByMonthAsync(
        StockReportFilter filter,
        CancellationToken ct)
    {
        var query = ApplyCommonTransactionFilters(
            context.StockTransactions.AsNoTracking()
                .Where(t =>
                    t.Type == TransactionType.ProjectConsumption ||
                    t.Type == TransactionType.ExceptionOut),
            filter);

        var entries = await query
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, Material = m })
            .ToListAsync(ct);

        var rows = entries
            .GroupBy(x => new
            {
                Year = x.Transaction.TransactedAt.Year,
                Month = x.Transaction.TransactedAt.Month,
                MaterialId = x.Material.Id.Value,
                x.Material.Name,
                x.Material.Unit,
                x.Transaction.Type
            })
            .Select(g => new StockOutflowsByMonthRow(
                g.Key.Year,
                g.Key.Month,
                g.Key.MaterialId,
                g.Key.Name,
                g.Key.Unit.ToString(),
                g.Key.Type.ToString(),
                g.Sum(x => x.Transaction.Quantity.Value),
                g.Count()))
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.MaterialName)
            .ToList();

        return new StockOutflowsByMonthReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
            rows);
    }

    public async Task<StockOutflowTotalsReport> GetStockOutflowTotalsAsync(
        StockReportFilter filter,
        CancellationToken ct)
    {
        var query = ApplyCommonTransactionFilters(
            context.StockTransactions.AsNoTracking()
                .Where(t =>
                    t.Type == TransactionType.ProjectConsumption ||
                    t.Type == TransactionType.ExceptionOut),
            filter);

        var entries = await query
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, Material = m })
            .Join(
                context.MaterialTypes.AsNoTracking(),
                x => x.Material.TypeId,
                mt => mt.Id,
                (x, mt) => new { x.Transaction, x.Material, MaterialTypeName = mt.Name })
            .ToListAsync(ct);

        var totals = entries
            .GroupBy(x => new
            {
                MaterialId = x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                x.Material.Unit
            })
            .Select(g => new
            {
                g.Key.MaterialId,
                MaterialName = g.Key.Name,
                g.Key.MaterialTypeName,
                Unit = g.Key.Unit.ToString(),
                ProjectConsumptionQuantity = g
                    .Where(x => x.Transaction.Type == TransactionType.ProjectConsumption)
                    .Sum(x => x.Transaction.Quantity.Value),
                ExceptionOutQuantity = g
                    .Where(x => x.Transaction.Type == TransactionType.ExceptionOut)
                    .Sum(x => x.Transaction.Quantity.Value)
            })
            .OrderBy(x => x.MaterialName)
            .ToList();

        var totalQuantity = totals.Sum(x => x.ProjectConsumptionQuantity + x.ExceptionOutQuantity);

        return new StockOutflowTotalsReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
            totals.Select(x =>
            {
                var rowTotal = x.ProjectConsumptionQuantity + x.ExceptionOutQuantity;
                var participation = totalQuantity == 0 ? 0 : Math.Round(rowTotal / totalQuantity * 100, 2);

                return new StockOutflowTotalsRow(
                    x.MaterialId,
                    x.MaterialName,
                    x.MaterialTypeName,
                    x.Unit,
                    x.ProjectConsumptionQuantity,
                    x.ExceptionOutQuantity,
                    rowTotal,
                    participation);
            }).ToList());
    }

    public async Task<StockInflowsByOrderMaterialMonthReport> GetStockInflowsByOrderMaterialMonthAsync(
        StockReportFilter filter,
        CancellationToken ct)
    {
        var query = ApplyCommonTransactionFilters(
            context.StockTransactions.AsNoTracking()
                .Where(t =>
                    t.Type == TransactionType.OrderReceipt ||
                    t.Type == TransactionType.ExceptionIn),
            filter);

        var entries = await query
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, Material = m })
            .ToListAsync(ct);

        var rows = entries
            .GroupBy(x => new
            {
                Year = x.Transaction.TransactedAt.Year,
                Month = x.Transaction.TransactedAt.Month,
                OrderId = x.Transaction.OrderId.HasValue ? x.Transaction.OrderId.Value.Value : (Guid?)null,
                MaterialId = x.Material.Id.Value,
                x.Material.Name,
                x.Material.Unit,
                x.Transaction.Type
            })
            .Select(g => new StockInflowsByOrderMaterialMonthRow(
                g.Key.Year,
                g.Key.Month,
                g.Key.OrderId,
                g.Key.MaterialId,
                g.Key.Name,
                g.Key.Unit.ToString(),
                g.Key.Type.ToString(),
                g.Sum(x => x.Transaction.Quantity.Value),
                g.Count()))
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.MaterialName)
            .ToList();

        return new StockInflowsByOrderMaterialMonthReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
            rows);
    }

    public async Task<CriticalStockBalanceReport> GetCriticalStockBalanceAsync(
        CriticalStockBalanceFilter filter,
        CancellationToken ct)
    {
        var query = context.Materials.AsNoTracking();

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(m => m.Id == materialId);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            query = query.Where(m => m.TypeId == materialTypeId);
        }

        var materials = await query
            .Join(
                context.MaterialTypes.AsNoTracking(),
                m => m.TypeId,
                mt => mt.Id,
                (m, mt) => new { Material = m, MaterialTypeName = mt.Name })
            .ToListAsync(ct);

        var rows = materials
            .Select(x => new CriticalStockBalanceRow(
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                x.Material.Location,
                x.Material.Unit.ToString(),
                x.Material.StockQuantity.Value,
                x.Material.MinStock.Value,
                x.Material.StockQuantity.Value - x.Material.MinStock.Value))
            .OrderBy(x => x.Difference)
            .ThenBy(x => x.MaterialName)
            .ToList();

        if (filter.OnlyCritical)
            rows = rows.Where(row => row.StockQuantity <= row.MinStock).ToList();

        return new CriticalStockBalanceReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.MaterialId,
            filter.MaterialTypeId,
            filter.OnlyCritical,
            rows);
    }

    public async Task<MaterialAuditMovementsReport> GetMaterialAuditMovementsAsync(
        MaterialAuditMovementsFilter filter,
        CancellationToken ct)
    {
        var transactionQuery = context.StockTransactions.AsNoTracking().AsQueryable();

        if (filter.From.HasValue)
            transactionQuery = transactionQuery.Where(t => t.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            transactionQuery = transactionQuery.Where(t => t.TransactedAt <= filter.To.Value);

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            transactionQuery = transactionQuery.Where(t => t.MaterialId == materialId);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType) &&
            Enum.TryParse<TransactionType>(filter.TransactionType, true, out var parsedType))
        {
            transactionQuery = transactionQuery.Where(t => t.Type == parsedType);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            transactionQuery = transactionQuery
                .Join(
                    context.Materials.AsNoTracking().Where(m => m.TypeId == materialTypeId),
                    t => t.MaterialId,
                    m => m.Id,
                    (t, _) => t);
        }

        var entries = await transactionQuery
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, Material = m })
            .Join(
                context.MaterialTypes.AsNoTracking(),
                x => x.Material.TypeId,
                mt => mt.Id,
                (x, mt) => new { x.Transaction, x.Material, MaterialTypeName = mt.Name })
            .OrderByDescending(x => x.Transaction.TransactedAt)
            .Take(filter.EffectiveLimit)
            .Select(x => new
            {
                x.Transaction.Id,
                MaterialId = x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                Unit = x.Material.Unit.ToString(),
                Type = x.Transaction.Type.ToString(),
                Quantity = x.Transaction.Quantity.Value,
                x.Transaction.TransactedAt,
                TransactedByUserId = x.Transaction.TransactedByUserId.Value,
                ProjectId = x.Transaction.ProjectId.HasValue ? x.Transaction.ProjectId.Value.Value : (Guid?)null,
                OrderId = x.Transaction.OrderId.HasValue ? x.Transaction.OrderId.Value.Value : (Guid?)null,
                x.Transaction.Justification
            })
            .ToListAsync(ct);

        var userNames = await userCatalog.GetUserDisplayNamesAsync(
            entries.Select(e => e.TransactedByUserId).Distinct(),
            ct);

        var projectTitles = await projectCatalog.GetProjectTitlesAsync(
            entries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct(),
            ct);

        var rows = entries.Select(e => new MaterialAuditMovementRow(
                e.Id.Value,
                e.MaterialId,
                e.Name,
                e.MaterialTypeName,
                e.Unit,
                e.Type,
                e.Quantity,
                e.TransactedAt,
                e.TransactedByUserId,
                userNames.GetValueOrDefault(e.TransactedByUserId, RemovedUserFallback),
                e.ProjectId,
                e.ProjectId.HasValue ? projectTitles.GetValueOrDefault(e.ProjectId.Value, RemovedProjectFallback) : null,
                e.OrderId,
                e.Justification))
            .ToList();

        return new MaterialAuditMovementsReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
            filter.TransactionType,
            filter.EffectiveLimit,
            rows);
    }

    public async Task<ManualStockAdjustmentsReport> GetManualStockAdjustmentsAsync(
        StockReportFilter filter,
        CancellationToken ct)
    {
        var query = ApplyCommonTransactionFilters(
            context.StockTransactions.AsNoTracking()
                .Where(t =>
                    t.Type == TransactionType.ExceptionIn ||
                    t.Type == TransactionType.ExceptionOut),
            filter);

        var entries = await query
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, Material = m })
            .Join(
                context.MaterialTypes.AsNoTracking(),
                x => x.Material.TypeId,
                mt => mt.Id,
                (x, mt) => new { x.Transaction, x.Material, MaterialTypeName = mt.Name })
            .OrderByDescending(x => x.Transaction.TransactedAt)
            .Select(x => new
            {
                x.Transaction.Id,
                MaterialId = x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                Unit = x.Material.Unit.ToString(),
                Type = x.Transaction.Type.ToString(),
                Quantity = x.Transaction.Quantity.Value,
                x.Transaction.TransactedAt,
                TransactedByUserId = x.Transaction.TransactedByUserId.Value,
                x.Transaction.Justification
            })
            .ToListAsync(ct);

        var userNames = await userCatalog.GetUserDisplayNamesAsync(
            entries.Select(e => e.TransactedByUserId).Distinct(),
            ct);

        var rows = entries.Select(e => new ManualStockAdjustmentRow(
                e.Id.Value,
                e.MaterialId,
                e.Name,
                e.MaterialTypeName,
                e.Unit,
                e.Type,
                e.Quantity,
                e.TransactedAt,
                e.TransactedByUserId,
                userNames.GetValueOrDefault(e.TransactedByUserId, RemovedUserFallback),
                e.Justification))
            .ToList();

        return new ManualStockAdjustmentsReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
            rows);
    }

    public async Task<StockMovementsByUserReport> GetStockMovementsByUserAsync(
        StockMovementsByUserFilter filter,
        CancellationToken ct)
    {
        var query = context.StockTransactions.AsNoTracking().AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(t => t.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.TransactedAt <= filter.To.Value);

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(t => t.MaterialId == materialId);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType) &&
            Enum.TryParse<TransactionType>(filter.TransactionType, true, out var parsedType))
        {
            query = query.Where(t => t.Type == parsedType);
        }

        var transactions = await query.ToListAsync(ct);

        var grouped = transactions
            .GroupBy(t => new { UserId = t.TransactedByUserId.Value, t.Type })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Type,
                TotalQuantity = g.Sum(t => t.Quantity.Value),
                MovementsCount = g.Count()
            })
            .ToList();

        var userNames = await userCatalog.GetUserDisplayNamesAsync(
            grouped.Select(g => g.UserId).Distinct(),
            ct);

        var rows = grouped
            .Select(g => new StockMovementsByUserRow(
                g.UserId,
                userNames.GetValueOrDefault(g.UserId, RemovedUserFallback),
                g.Type.ToString(),
                g.TotalQuantity,
                g.MovementsCount))
            .OrderBy(r => r.UserName)
            .ThenBy(r => r.TransactionType)
            .ToList();

        return new StockMovementsByUserReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.TransactionType,
            rows);
    }

    public async Task<IdleStockReport> GetIdleStockAsync(IdleStockFilter filter, CancellationToken ct)
    {
        var materialsQuery = context.Materials.AsNoTracking().AsQueryable();

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            materialsQuery = materialsQuery.Where(m => m.TypeId == materialTypeId);
        }

        var materials = (await materialsQuery
            .Join(
                context.MaterialTypes.AsNoTracking(),
                m => m.TypeId,
                mt => mt.Id,
                (m, mt) => new { Material = m, MaterialTypeName = mt.Name })
            .ToListAsync(ct))
            .Where(x => x.Material.StockQuantity.Value > 0)
            .ToList();

        var materialIds = materials.Select(x => x.Material.Id).ToList();

        var relevantTransactions = await context.StockTransactions.AsNoTracking()
            .Where(t => materialIds.Contains(t.MaterialId))
            .Select(t => new { t.MaterialId, t.TransactedAt })
            .ToListAsync(ct);

        var lastMovementMap = relevantTransactions
            .GroupBy(t => t.MaterialId.Value)
            .ToDictionary(g => g.Key, g => g.Max(t => t.TransactedAt));

        var since = filter.EffectiveSince;

        var rows = materials
            .Select(x =>
            {
                var lastMovementAt = lastMovementMap.TryGetValue(x.Material.Id.Value, out var found)
                    ? found
                    : (DateTime?)null;
                return new { x.Material, x.MaterialTypeName, LastMovementAt = lastMovementAt };
            })
            .Where(x => x.LastMovementAt is null || x.LastMovementAt < since)
            .Select(x => new IdleStockRow(
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                x.Material.Location,
                x.Material.Unit.ToString(),
                x.Material.StockQuantity.Value,
                x.LastMovementAt))
            .OrderBy(r => r.LastMovementAt ?? DateTime.MinValue)
            .ThenBy(r => r.MaterialName)
            .ToList();

        return new IdleStockReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.MaterialTypeId,
            since,
            rows);
    }

    public async Task<OrderStatusCycleReport> GetOrderStatusCycleAsync(OrderStatusCycleFilter filter, CancellationToken ct)
    {
        var query = context.Orders.AsNoTracking().AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(o => EF.Property<DateTimeOffset>(o, "CreatedAt") >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(o => EF.Property<DateTimeOffset>(o, "CreatedAt") <= filter.To.Value);

        var orders = await query
            .Join(
                context.Materials.AsNoTracking(),
                o => o.MaterialId,
                m => m.Id,
                (o, m) => new { Order = o, MaterialName = m.Name, CreatedAt = EF.Property<DateTimeOffset>(o, "CreatedAt") })
            .ToListAsync(ct);

        var statusCounts = orders
            .GroupBy(x => x.Order.Status)
            .Select(g => new OrderStatusCountRow(g.Key.ToString(), g.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        var pendingToProcessingHours = orders
            .Where(x => x.Order.Processing is not null)
            .Select(x => (x.Order.Processing!.ProcessedAt - x.CreatedAt).TotalHours)
            .ToList();

        var processingToCompletedHours = orders
            .Where(x => x.Order.Processing is not null && x.Order.Receipt is not null)
            .Select(x => (x.Order.Receipt!.ReceivedAt - x.Order.Processing!.ProcessedAt).TotalHours)
            .ToList();

        var now = DateTimeOffset.UtcNow;

        var staleOrders = orders
            .Where(x => x.Order.Status is OrderStatus.Pending or OrderStatus.Processing)
            .Select(x =>
            {
                var lastTransitionAt = x.Order.Status == OrderStatus.Processing
                    ? x.Order.Processing!.ProcessedAt
                    : x.CreatedAt;

                return new
                {
                    x.Order.Id,
                    x.MaterialName,
                    x.Order.Status,
                    LastTransitionAt = lastTransitionAt,
                    DaysInStatus = (now - lastTransitionAt).TotalDays
                };
            })
            .Where(x => x.DaysInStatus > filter.EffectiveStaleDays)
            .OrderByDescending(x => x.DaysInStatus)
            .Select(x => new StaleOrderRow(
                x.Id.Value,
                x.MaterialName,
                x.Status.ToString(),
                x.LastTransitionAt.UtcDateTime,
                (int)x.DaysInStatus))
            .ToList();

        return new OrderStatusCycleReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.EffectiveStaleDays,
            statusCounts,
            pendingToProcessingHours.Count == 0 ? null : Math.Round(pendingToProcessingHours.Average(), 2),
            processingToCompletedHours.Count == 0 ? null : Math.Round(processingToCompletedHours.Average(), 2),
            staleOrders);
    }

    public async Task<StockByMaterialTypeReport> GetStockByMaterialTypeAsync(StockByMaterialTypeFilter filter, CancellationToken ct)
    {
        var transactionsQuery = context.StockTransactions.AsNoTracking().AsQueryable();

        if (filter.From.HasValue)
            transactionsQuery = transactionsQuery.Where(t => t.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            transactionsQuery = transactionsQuery.Where(t => t.TransactedAt <= filter.To.Value);

        var transactionsWithType = await transactionsQuery
            .Join(
                context.Materials.AsNoTracking(),
                t => t.MaterialId,
                m => m.Id,
                (t, m) => new { Transaction = t, m.TypeId })
            .ToListAsync(ct);

        var movementsByType = transactionsWithType
            .GroupBy(x => x.TypeId.Value)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    InflowQuantity = g
                        .Where(x => x.Transaction.Type == TransactionType.OrderReceipt || x.Transaction.Type == TransactionType.ExceptionIn)
                        .Sum(x => x.Transaction.Quantity.Value),
                    OutflowQuantity = g
                        .Where(x => x.Transaction.Type == TransactionType.ProjectConsumption || x.Transaction.Type == TransactionType.ExceptionOut)
                        .Sum(x => x.Transaction.Quantity.Value)
                });

        var materialsByType = (await context.Materials.AsNoTracking().ToListAsync(ct))
            .GroupBy(m => m.TypeId.Value)
            .Select(g => new
            {
                MaterialTypeId = g.Key,
                MaterialsCount = g.Count(),
                CurrentStockQuantity = g.Sum(m => m.StockQuantity.Value)
            })
            .ToList();

        var materialTypeNames = await context.MaterialTypes.AsNoTracking()
            .ToDictionaryAsync(mt => mt.Id.Value, mt => mt.Name, ct);

        var rows = materialsByType
            .Select(x =>
            {
                movementsByType.TryGetValue(x.MaterialTypeId, out var movement);
                var inflow = movement?.InflowQuantity ?? 0;
                var outflow = movement?.OutflowQuantity ?? 0;

                return new StockByMaterialTypeRow(
                    x.MaterialTypeId,
                    materialTypeNames.GetValueOrDefault(x.MaterialTypeId, "Tipo removido"),
                    inflow,
                    outflow,
                    inflow - outflow,
                    x.CurrentStockQuantity,
                    x.MaterialsCount);
            })
            .OrderByDescending(r => r.CurrentStockQuantity)
            .ToList();

        return new StockByMaterialTypeReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            rows);
    }

    private IQueryable<StockTransaction> ApplyCommonTransactionFilters(
        IQueryable<StockTransaction> query,
        StockReportFilter filter)
    {
        if (filter.From.HasValue)
            query = query.Where(t => t.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.TransactedAt <= filter.To.Value);

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(t => t.MaterialId == materialId);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            query = query
                .Join(
                    context.Materials.AsNoTracking().Where(m => m.TypeId == materialTypeId),
                    t => t.MaterialId,
                    m => m.Id,
                    (t, _) => t);
        }

        return query;
    }
}
