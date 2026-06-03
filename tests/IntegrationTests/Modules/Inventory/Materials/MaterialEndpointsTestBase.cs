using System;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.IntegrationTests;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Materials;

public abstract class MaterialEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/inventory/materials";

    protected MaterialEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedMaterialAsync(decimal stock = 100m)
        => MaterialDataSeeder.SeedMaterialAsync(DbContext, stock);
}
