using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Reports;

internal sealed class StockReportQueries(
    InventoryDbContext context,
    IProjectCatalog projectCatalog) : IStockReportQueries
{
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

        var rows = await transactionQuery
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
            .Select(x => new MaterialAuditMovementRow(
                x.Transaction.Id.Value,
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                x.Material.Unit.ToString(),
                x.Transaction.Type.ToString(),
                x.Transaction.Quantity.Value,
                x.Transaction.TransactedAt,
                x.Transaction.TransactedByUserId.Value,
                x.Transaction.ProjectId.HasValue ? x.Transaction.ProjectId.Value.Value : (Guid?)null,
                x.Transaction.OrderId.HasValue ? x.Transaction.OrderId.Value.Value : (Guid?)null,
                x.Transaction.Justification))
            .ToListAsync(ct);

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

        var rows = await query
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
            .Select(x => new ManualStockAdjustmentRow(
                x.Transaction.Id.Value,
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialTypeName,
                x.Material.Unit.ToString(),
                x.Transaction.Type.ToString(),
                x.Transaction.Quantity.Value,
                x.Transaction.TransactedAt,
                x.Transaction.TransactedByUserId.Value,
                x.Transaction.Justification))
            .ToListAsync(ct);

        return new ManualStockAdjustmentsReport(
            DateTimeOffset.UtcNow.UtcDateTime,
            filter.From,
            filter.To,
            filter.MaterialId,
            filter.MaterialTypeId,
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
