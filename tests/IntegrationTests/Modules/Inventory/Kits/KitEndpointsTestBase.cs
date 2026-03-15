using LabViroMol.Modules.Inventory.IntegrationTests;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Kits;

public abstract class KitEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/inventory/kits";

    protected KitEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<(Guid kitId, Guid materialId)> SeedKitAsync()
        => KitDataSeeder.SeedKitAsync(DbContext);
}
