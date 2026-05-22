using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Abstractions.Identity;

using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Orders;

public static class OrderDataSeeder
{
    public static async Task<(Guid materialId, Guid orderId)> SeedPendingOrderAsync(InventoryDbContext dbContext)
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(dbContext, stock: 100m);

        var order = Order.Create(
            MaterialId.From(materialId),
            ProjectId.From(Guid.NewGuid()),
            IdFactory.New<UserId>(),
            (Quantity)10m,
            "Pedido de teste");

        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        return (materialId, order.Id.Value);
    }
}
