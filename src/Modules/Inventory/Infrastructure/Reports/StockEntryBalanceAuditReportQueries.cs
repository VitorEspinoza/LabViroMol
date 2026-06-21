using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Reports;

public sealed class StockEntryBalanceAuditReportQueries : IStockEntryBalanceAuditReportQueries
{
    private const int DefaultAuditRowsLimit = 500;
    private const int MaxAuditRowsLimit = 5_000;

    private readonly InventoryDbContext _context;

    public StockEntryBalanceAuditReportQueries(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<StockInflowByOrderMaterialMonthReportItem>> GetStockInflowsByOrderMaterialMonthAsync(
        StockInflowByOrderMaterialMonthReportFilter filter,
        CancellationToken ct = default)
    {
        var query =
            from transaction in _context.StockTransactions.AsNoTracking()
            join material in _context.Materials.AsNoTracking() on transaction.MaterialId equals material.Id
            join materialType in _context.MaterialTypes.AsNoTracking() on material.TypeId equals materialType.Id
            where transaction.Type == TransactionType.OrderReceipt ||
                  transaction.Type == TransactionType.ExceptionIn
            select new
            {
                Transaction = transaction,
                Material = material,
                MaterialType = materialType
            };

        if (filter.From.HasValue)
            query = query.Where(x => x.Transaction.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(x => x.Transaction.TransactedAt <= filter.To.Value);

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(x => x.Material.Id == materialId);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            query = query.Where(x => x.Material.TypeId == materialTypeId);
        }

        return await query
            .GroupBy(x => new
            {
                Year = x.Transaction.TransactedAt.Year,
                Month = x.Transaction.TransactedAt.Month,
                x.Material.Id,
                x.Material.Name,
                TypeId = x.MaterialType.Id,
                TypeName = x.MaterialType.Name,
                x.Material.Unit,
                x.Transaction.Type,
                x.Transaction.OrderId
            })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .ThenBy(x => x.Key.Name)
            .ThenBy(x => x.Key.Type)
            .Select(x => new StockInflowByOrderMaterialMonthReportItem(
                x.Key.Year,
                x.Key.Month,
                x.Key.Id.Value,
                x.Key.Name,
                x.Key.TypeId.Value,
                x.Key.TypeName,
                x.Key.Unit.ToString(),
                x.Key.Type.ToString(),
                x.Key.OrderId != null ? x.Key.OrderId.Value.Value : null,
                x.Count(),
                x.Sum(y => y.Transaction.Quantity.Value)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<CriticalStockBalanceReportItem>> GetCriticalStockBalanceAsync(
        CriticalStockBalanceReportFilter filter,
        CancellationToken ct = default)
    {
        var query =
            from material in _context.Materials.AsNoTracking()
            join materialType in _context.MaterialTypes.AsNoTracking() on material.TypeId equals materialType.Id
            select new
            {
                Material = material,
                MaterialType = materialType
            };

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(x => x.Material.Id == materialId);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            query = query.Where(x => x.Material.TypeId == materialTypeId);
        }

        if (filter.OnlyCritical)
            query = query.Where(x => x.Material.StockQuantity.Value <= x.Material.MinStock.Value);

        return await query
            .OrderBy(x => x.Material.Name)
            .Select(x => new CriticalStockBalanceReportItem(
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialType.Id.Value,
                x.MaterialType.Name,
                x.Material.Location,
                x.Material.Unit.ToString(),
                x.Material.StockQuantity.Value,
                x.Material.MinStock.Value,
                x.Material.StockQuantity.Value - x.Material.MinStock.Value))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<MaterialAuditMovementReportItem>> GetMaterialAuditMovementsAsync(
        MaterialAuditMovementsReportFilter filter,
        CancellationToken ct = default)
    {
        var query =
            from transaction in _context.StockTransactions.AsNoTracking()
            join material in _context.Materials.AsNoTracking() on transaction.MaterialId equals material.Id
            join materialType in _context.MaterialTypes.AsNoTracking() on material.TypeId equals materialType.Id
            select new
            {
                Transaction = transaction,
                Material = material,
                MaterialType = materialType
            };

        if (filter.From.HasValue)
            query = query.Where(x => x.Transaction.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(x => x.Transaction.TransactedAt <= filter.To.Value);

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(x => x.Material.Id == materialId);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            query = query.Where(x => x.Material.TypeId == materialTypeId);
        }

        if (filter.TransactionType.HasValue)
            query = query.Where(x => x.Transaction.Type == filter.TransactionType.Value);

        var limit = GetAuditRowsLimit(filter.MaxRows, hasPeriodFilter: filter.From.HasValue || filter.To.HasValue);

        return await query
            .OrderByDescending(x => x.Transaction.TransactedAt)
            .Take(limit)
            .Select(x => new MaterialAuditMovementReportItem(
                x.Transaction.Id.Value,
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialType.Id.Value,
                x.MaterialType.Name,
                x.Material.Unit.ToString(),
                x.Transaction.Type.ToString(),
                x.Transaction.Quantity.Value,
                x.Transaction.TransactedAt,
                x.Transaction.TransactedByUserId.Value,
                x.Transaction.Justification,
                x.Transaction.OrderId != null ? x.Transaction.OrderId.Value.Value : null,
                x.Transaction.ProjectId != null ? x.Transaction.ProjectId.Value.Value : null))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<ManualStockAdjustmentReportItem>> GetManualStockAdjustmentsAsync(
        ManualStockAdjustmentsReportFilter filter,
        CancellationToken ct = default)
    {
        var query =
            from transaction in _context.StockTransactions.AsNoTracking()
            join material in _context.Materials.AsNoTracking() on transaction.MaterialId equals material.Id
            join materialType in _context.MaterialTypes.AsNoTracking() on material.TypeId equals materialType.Id
            where transaction.Type == TransactionType.ExceptionIn ||
                  transaction.Type == TransactionType.ExceptionOut
            select new
            {
                Transaction = transaction,
                Material = material,
                MaterialType = materialType
            };

        if (filter.From.HasValue)
            query = query.Where(x => x.Transaction.TransactedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(x => x.Transaction.TransactedAt <= filter.To.Value);

        if (filter.MaterialId.HasValue)
        {
            var materialId = MaterialId.From(filter.MaterialId.Value);
            query = query.Where(x => x.Material.Id == materialId);
        }

        if (filter.MaterialTypeId.HasValue)
        {
            var materialTypeId = MaterialTypeId.From(filter.MaterialTypeId.Value);
            query = query.Where(x => x.Material.TypeId == materialTypeId);
        }

        var limit = GetAuditRowsLimit(filter.MaxRows, hasPeriodFilter: filter.From.HasValue || filter.To.HasValue);

        return await query
            .OrderByDescending(x => x.Transaction.TransactedAt)
            .Take(limit)
            .Select(x => new ManualStockAdjustmentReportItem(
                x.Transaction.Id.Value,
                x.Material.Id.Value,
                x.Material.Name,
                x.MaterialType.Id.Value,
                x.MaterialType.Name,
                x.Material.Unit.ToString(),
                x.Transaction.Type.ToString(),
                x.Transaction.Quantity.Value,
                x.Transaction.TransactedAt,
                x.Transaction.TransactedByUserId.Value,
                x.Transaction.Justification ?? string.Empty))
            .ToListAsync(ct);
    }

    private static int GetAuditRowsLimit(int? requestedLimit, bool hasPeriodFilter)
    {
        if (requestedLimit.HasValue)
            return Math.Clamp(requestedLimit.Value, 1, MaxAuditRowsLimit);

        return hasPeriodFilter ? MaxAuditRowsLimit : DefaultAuditRowsLimit;
    }
}
