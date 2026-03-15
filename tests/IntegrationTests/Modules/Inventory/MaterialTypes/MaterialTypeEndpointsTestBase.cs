using LabViroMol.Modules.Inventory.IntegrationTests;

namespace LabViroMol.Modules.Inventory.IntegrationTests.MaterialTypes;

public abstract class MaterialTypeEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/inventory/types";

    protected MaterialTypeEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedMaterialTypeAsync()
        => MaterialTypeDataSeeder.SeedMaterialTypeAsync(DbContext);
}
