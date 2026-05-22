using LabViroMol.Modules.Research.IntegrationTests;

namespace LabViroMol.Modules.Research.IntegrationTests.Positions;

public abstract class PositionEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/research/positions";

    protected PositionEndpointsTestBase(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedPositionAsync()
        => PositionDataSeeder.SeedPositionAsync(DbContext);
}