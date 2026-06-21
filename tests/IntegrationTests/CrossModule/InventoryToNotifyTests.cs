using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Material = LabViroMol.Modules.Inventory.Domain.Materials.Material;
using Unit = LabViroMol.Modules.Inventory.Domain.Materials.Unit;

namespace LabViroMol.IntegrationTests.CrossModule;

public class InventoryToNotifyTests : BaseCrossModuleTest
{
    public InventoryToNotifyTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAs([Permissions.Inventory.StockManage]);
    }

    private async Task<Guid> SeedMaterialAsync(decimal minStock, decimal stockQuantity)
    {
        var materialType = MaterialType.Create("Reagente de Teste");
        await InventoryDbContext.MaterialTypes.AddAsync(materialType);

        var material = Material.Create(
            "Reagente B", "Estante 2", new Quantity(minStock), new Quantity(stockQuantity), Unit.Milliliter, materialType).Data!;
        await InventoryDbContext.Materials.AddAsync(material);
        await InventoryDbContext.SaveChangesAsync();

        return material.Id.Value;
    }

    [Fact]
    public async Task ShouldCreateNotification_WhenStockDropsToOrBelowMinimum()
    {
        var materialId = await SeedMaterialAsync(minStock: 10, stockQuantity: 15);

        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(10, null, "Baixa para gerar alerta de estoque mínimo."));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notification = await NotifyDbContext.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.ReferenceId == materialId.ToString());

        Assert.NotNull(notification);
        Assert.Equal("Inventory", notification!.ReferenceModule);
        Assert.Equal(Permissions.Inventory.MaterialsManage, notification.TargetPermission);
    }

    [Fact]
    public async Task ShouldNotCreateNotification_WhenStockStaysAboveMinimum()
    {
        var materialId = await SeedMaterialAsync(minStock: 10, stockQuantity: 50);

        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(5, null, "Baixa de rotina."));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notificationExists = await NotifyDbContext.Notifications.AsNoTracking()
            .AnyAsync(n => n.ReferenceId == materialId.ToString());

        Assert.False(notificationExists);
    }
}
