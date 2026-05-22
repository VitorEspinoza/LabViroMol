using LabViroMol.Modules.Research.IntegrationTests;

namespace LabViroMol.Modules.Research.IntegrationTests.Projects;

public abstract class ProjectEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/research/projects";

    protected ProjectEndpointsTestBase(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<(Guid projectId, Guid leadResearcherId, Guid partnerId)> SeedProjectAsync()
        => ProjectDataSeeder.SeedProjectAsync(DbContext);

    protected Task<(Guid projectId, Guid leadResearcherId, Guid partnerId)> SeedStartedProjectAsync()
        => ProjectDataSeeder.SeedStartedProjectAsync(DbContext);
}