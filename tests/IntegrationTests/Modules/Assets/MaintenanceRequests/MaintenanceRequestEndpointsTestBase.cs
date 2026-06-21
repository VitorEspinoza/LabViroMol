using LabViroMol.Modules.Assets.IntegrationTests;

namespace LabViroMol.Modules.Assets.IntegrationTests.MaintenanceRequests;

public abstract class MaintenanceRequestEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/assets/maintenance-requests";

    protected MaintenanceRequestEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<(Guid equipmentId, Guid maintenanceRequestId)> SeedRequestedAsync()
        => MaintenanceRequestDataSeeder.SeedRequestedAsync(DbContext);
}
