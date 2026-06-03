using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Application.Materials.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Materials;

public class MaterialQueries
{
    private readonly InventoryDbContext _context;

    public MaterialQueries(InventoryDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<List<MaterialViewModel>> GetAll()
    {
        return await _context.Materials
            .Join(
                _context.MaterialTypes,
                m => m.TypeId,
                t => t.Id,
                (m, t) => new MaterialViewModel(
                    m.Id.Value,
                    m.Name,
                    t.Name,
                    m.MinStock.Value,
                    m.StockQuantity.Value,
                    m.Unit.ToString(),
                    m.Location))
            .ToListAsync();
    }

    public async Task<MaterialViewModel?> GetById(Guid id)
    {
        return await _context.Materials
            .Where(m => m.Id == MaterialId.From(id))
            .Join(
                _context.MaterialTypes,
                m => m.TypeId,
                t => t.Id,
                (m, t) => new MaterialViewModel(
                    m.Id.Value,
                    m.Name,
                    t.Name,
                    m.MinStock.Value,
                    m.StockQuantity.Value,
                    m.Unit.ToString(),
                    m.Location))
            .FirstOrDefaultAsync();
    }
}
