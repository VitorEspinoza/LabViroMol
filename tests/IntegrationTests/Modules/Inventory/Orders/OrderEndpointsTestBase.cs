using LabViroMol.Modules.Inventory.IntegrationTests;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Orders;

public abstract class OrderEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/inventory/orders";

    protected OrderEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<(Guid materialId, Guid orderId)> SeedPendingOrderAsync()
        => OrderDataSeeder.SeedPendingOrderAsync(DbContext);
}
