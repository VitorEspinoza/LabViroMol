using LabViroMol.Modules.Research.IntegrationTests;

namespace LabViroMol.Modules.Research.IntegrationTests.Researchers;

public abstract class ResearcherEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/research/researchers";

    protected ResearcherEndpointsTestBase(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<(Guid researcherId, Guid positionId)> SeedResearcherAsync()
        => ResearcherDataSeeder.SeedResearcherAsync(DbContext);
}