using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.IntegrationTests.MaterialTypes;

public static class MaterialTypeDataSeeder
{
    public static async Task<Guid> SeedMaterialTypeAsync(InventoryDbContext dbContext)
    {
        var type = MaterialType.Create(IdFactory.New<UserId>(), "Tipo Teste");
        await dbContext.MaterialTypes.AddAsync(type);
        await dbContext.SaveChangesAsync();
        return type.Id.Value;
    }
}
