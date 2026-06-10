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

    [Fact]
    public async Task TEMP_ShouldReturn200_WhenSearchingAndSorting()
    {
        await SeedResearcherAsync();

        var responseSearchFirstName = await Client.GetAsync($"{BaseRoute}?search=Ana");
        var responseSearchLastName = await Client.GetAsync($"{BaseRoute}?search=Silva");
        var responseSortPosition = await Client.GetAsync($"{BaseRoute}?sortBy=position&sortDirection=desc");
        var responseSortName = await Client.GetAsync($"{BaseRoute}?sortBy=name&sortDirection=desc");

        Assert.Equal(HttpStatusCode.OK, responseSearchFirstName.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseSearchLastName.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseSortPosition.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseSortName.StatusCode);
    }
}