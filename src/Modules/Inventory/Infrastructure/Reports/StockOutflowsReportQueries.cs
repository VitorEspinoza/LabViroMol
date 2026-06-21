using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Application.Reports.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Reports;

public class StockOutflowsReportQueries : IStockOutflowsReportQueries
{
    private readonly InventoryDbContext _context;
    private readonly IProjectCatalog _projectCatalog;

    public StockOutflowsReportQueries(InventoryDbContext context, IProjectCatalog projectCatalog)
    {
        _context = context;
        _projectCatalog = projectCatalog;
    }

    public async Task<IReadOnlyList<StockOutflowsByProjectViewModel>> GetByProjectAsync(
        StockOutflowsReportFilter filter,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(CreateBaseQuery(), filter)
            .Where(x => x.Transaction.Type == TransactionType.ProjectConsumption && x.Transaction.ProjectId != null);

        if (filter.ProjectId.HasValue)
        {
            var projectId = ProjectId.From(filter.ProjectId.Value);
            query = query.Where(x => x.Transaction.ProjectId == projectId);
        }

        var rows = await query
            .GroupBy(x => new
            {
                ProjectId = x.Transaction.ProjectId!.Value.Value,
                MaterialId = x.Material.Id.Value,
                MaterialName = x.Material.Name,
                MaterialType = x.MaterialType.Name,
                Unit = x.Material.Unit
            })
            .Select(g => new
            {
                g.Key.ProjectId,
                g.Key.MaterialId,
                g.Key.MaterialName,
                g.Key.MaterialType,
                Unit = g.Key.Unit.ToString(),
                Quantity = g.Sum(x => x.Transaction.Quantity.Value)
            })
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.MaterialName)
            .ToListAsync(ct);

        var projectTitles = await _projectCatalog.GetProjectTitlesAsync(rows.Select(r => r.ProjectId), ct);

        return rows.Select(r => new StockOutflowsByProjectViewModel(
                r.ProjectId,
                projectTitles.GetValueOrDefault(r.ProjectId, string.Empty),
                r.MaterialId,
                r.MaterialName,
                r.MaterialType,
                r.Unit,
                r.Quantity))
            .ToList();
    }

    public async Task<IReadOnlyList<StockOutflowsByMonthViewModel>> GetByMonthAsync(
        StockOutflowsReportFilter filter,
        CancellationToken ct = default)
    {
        var query = ApplyOutflowFilters(filter);

        if (filter.ProjectId.HasValue)
        {
            var projectId = ProjectId.From(filter.ProjectId.Value);
            query = query.Where(x => x.Transaction.Type == TransactionType.ProjectConsumption &&
                                     x.Transaction.ProjectId == projectId);
        }

        return await query
            .GroupBy(x => new
            {
                x.Transaction.TransactedAt.Year,
                x.Transaction.TransactedAt.Month,
                MaterialId = x.Material.Id.Value,
                MaterialName = x.Material.Name,
                MaterialType = x.MaterialType.Name,
                Unit = x.Material.Unit,
                x.Transaction.Type
            })
            .Select(g => new StockOutflowsByMonthViewModel(
                g.Key.Year,
                g.Key.Month,
                g.Key.MaterialId,
                g.Key.MaterialName,
                g.Key.MaterialType,
                g.Key.Unit.ToString(),
                g.Key.Type.ToString(),
                g.Sum(x => x.Transaction.Quantity.Value)))
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.MaterialName)
            .ThenBy(x => x.OutflowType)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockOutflowsTotalsViewModel>> GetTotalsAsync(
        StockOutflowsReportFilter filter,
        CancellationToken ct = default)
    {
        var query = ApplyOutflowFilters(filter);

        if (filter.ProjectId.HasValue)
        {
            var projectId = ProjectId.From(filter.ProjectId.Value);
            query = query.Where(x => x.Transaction.Type == TransactionType.ProjectConsumption &&
                                     x.Transaction.ProjectId == projectId);
        }

        return await query
            .GroupBy(x => new
            {
                MaterialId = x.Material.Id.Value,
                MaterialName = x.Material.Name,
                MaterialType = x.MaterialType.Name,
                Unit = x.Material.Unit
            })
            .Select(g => new StockOutflowsTotalsViewModel(
                g.Key.MaterialId,
                g.Key.MaterialName,
                g.Key.MaterialType,
                g.Key.Unit.ToString(),
                g.Where(x => x.Transaction.Type == TransactionType.ProjectConsumption)
                    .Sum(x => x.Transaction.Quantity.Value),
                g.Where(x => x.Transaction.Type == TransactionType.ExceptionOut)
                    .Sum(x => x.Transaction.Quantity.Value),
                g.Sum(x => x.Transaction.Quantity.Value)))
            .OrderBy(x => x.MaterialName)
            .ToListAsync(ct);
    }

    private IQueryable<StockOutflowReportRow> ApplyOutflowFilters(StockOutflowsReportFilter filter)
    {
        return ApplyFilters(CreateBaseQuery(), filter)
            .Where(x => x.Transaction.Type == TransactionType.ProjectConsumption ||
                        x.Transaction.Type == TransactionType.ExceptionOut);
    }

    private IQueryable<StockOutflowReportRow> CreateBaseQuery()
    {
        return _context.StockTransactions
            .AsNoTracking()
            .Join(
                _context.Materials.AsNoTracking(),
                transaction => transaction.MaterialId,
                material => material.Id,
                (transaction, material) => new { Transaction = transaction, Material = material })
            .Join(
                _context.MaterialTypes.AsNoTracking(),
                x => x.Material.TypeId,
                materialType => materialType.Id,
                (x, materialType) => new StockOutflowReportRow(x.Transaction, x.Material, materialType));
    }

    private static IQueryable<StockOutflowReportRow> ApplyFilters(
        IQueryable<StockOutflowReportRow> query,
        StockOutflowsReportFilter filter)
    {
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

        return query;
    }

    private sealed record StockOutflowReportRow(
        StockTransaction Transaction,
        Material Material,
        MaterialType MaterialType);
}
