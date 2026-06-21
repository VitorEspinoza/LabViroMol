using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Inventory.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : BaseIntegrationTest<InventoryDbContext>
{
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAsAdmin();
    }

    protected void AuthenticateAsAdmin()
    {
        AuthenticateAs(
        [
            Permissions.Inventory.MaterialsView, Permissions.Inventory.MaterialsManage,
            Permissions.Inventory.KitsView, Permissions.Inventory.KitsManage,
            Permissions.Inventory.OrdersView, Permissions.Inventory.OrdersManage,
            Permissions.Inventory.StockView, Permissions.Inventory.StockManage,
        ]);
    }
}
