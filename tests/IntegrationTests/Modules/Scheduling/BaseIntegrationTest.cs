using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Scheduling.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : BaseIntegrationTest<SchedulingDbContext>
{
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAsAdmin();
    }

    protected void AuthenticateAsAdmin() =>
        AuthenticateAs([Permissions.Scheduling.SchedulesView, Permissions.Scheduling.SchedulesManage]);
}
