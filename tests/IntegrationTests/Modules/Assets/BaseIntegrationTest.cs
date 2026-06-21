using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Assets.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : BaseIntegrationTest<AssetsDbContext>
{
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAsAdmin();
    }

    protected void AuthenticateAsAdmin()
    {
        AuthenticateAs(
        [
            Permissions.Assets.EquipmentsView, Permissions.Assets.EquipmentsManage,
            Permissions.Assets.MaintenanceView, Permissions.Assets.MaintenanceManage,
        ]);
    }
}
