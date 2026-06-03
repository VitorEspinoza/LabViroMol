using System;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.Equipments;

public class EquipmentRepository : IEquipmentRepository
{
    private readonly AssetsDbContext _context;
    
    public EquipmentRepository(AssetsDbContext context)
    {
        _context = context;
    }
    
    public async Task<Equipment?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return await _context.Equipments
            .FirstOrDefaultAsync(e => e.Code == code, ct);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken ct)
    {
        await _context.Equipments.AddAsync(equipment, ct);
    }

    public async Task<Equipment?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public void Remove(Equipment equipment)
    {
        _context.Equipments.Remove(equipment);
    }
}