using System.Net;

namespace LabViroMol.Modules.Research.IntegrationTests.Researchers;

public class GetResearcherTests : ResearcherEndpointsTestBase
{
    public GetResearcherTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenGettingAll()
    {
        await SeedResearcherAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}