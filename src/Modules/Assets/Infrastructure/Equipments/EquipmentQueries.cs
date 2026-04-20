using LabViroMol.Modules.Assets.Application.Equipments.ViewModels;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.Equipments;

public class EquipmentQueries
{
    private readonly AssetsDbContext _context;
    
    public EquipmentQueries(AssetsDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<List<EquipmentViewModel>> GetAllEquipments()
    {
        var equipments = await _context.Equipments
            .Select(equipment => new EquipmentViewModel(
                equipment.Id.Value,
                equipment.Name,
                equipment.Model,
                equipment.Brand,
                equipment.Code,
                equipment.Description))
            .ToListAsync();
        
        return equipments;
    }
}