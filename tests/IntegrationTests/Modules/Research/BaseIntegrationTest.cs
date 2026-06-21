using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Research.IntegrationTests;

[CollectionDefinition("ResearchIntegrationTests")]
public class ResearchIntegrationTestCollection : ICollectionFixture<ResearchIntegrationTestWebAppFactory> { }

[Collection("ResearchIntegrationTests")]
public abstract class BaseIntegrationTest : BaseIntegrationTest<ResearchDbContext>
{
    protected BaseIntegrationTest(ResearchIntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAsAdmin();
    }

    protected void AuthenticateAsAdmin()
    {
        AuthenticateAs(
        [
            Permissions.Research.ProjectsView, Permissions.Research.ProjectsManage,
            Permissions.Research.PublicationsView, Permissions.Research.PublicationsManage,
            Permissions.Research.ResearchersView, Permissions.Research.ResearchersManage,
            Permissions.Research.PartnersView, Permissions.Research.PartnersManage,
            Permissions.Research.PositionsView, Permissions.Research.PositionsManage,
        ]);
    }
}
