using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Materials;

public static class MaterialDataSeeder
{
    public static async Task<Guid> SeedMaterialAsync(InventoryDbContext dbContext, decimal stock = 100m)
    {
        var type = MaterialType.Create("Tipo Teste");
        await dbContext.MaterialTypes.AddAsync(type);

        var material = Material.Create("Material Teste", "Sala A",
            (Quantity)10m, (Quantity)stock, Unit.Gram, type).Data!;
            
        await dbContext.Materials.AddAsync(material);
        await dbContext.SaveChangesAsync();
        
        return material.Id.Value;
    }
}