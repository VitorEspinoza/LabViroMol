using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Abstractions.Identity;

using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Kits;

public static class KitDataSeeder
{
    public static async Task<(Guid kitId, Guid materialId)> SeedKitAsync(InventoryDbContext dbContext)
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(dbContext);

        var kit = Kit.Create(
            UserId.New(),
            "Kit Teste",
            "Descrição do kit de teste",
            new List<KitItem> { new KitItem(MaterialId.From(materialId), (Quantity)1m) });

        await dbContext.Kits.AddAsync(kit);
        await dbContext.SaveChangesAsync();

        return (kit.Id.Value, materialId);
    }
}
