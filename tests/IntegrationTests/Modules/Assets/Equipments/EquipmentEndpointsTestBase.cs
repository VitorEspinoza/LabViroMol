using LabViroMol.Modules.Assets.IntegrationTests;

namespace LabViroMol.Modules.Assets.IntegrationTests.Equipments;

public abstract class EquipmentEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/assets/equipments";

    protected EquipmentEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedEquipmentAsync(string? code = null, string? location = null)
        => EquipmentDataSeeder.SeedEquipmentAsync(DbContext, code, location);
}
