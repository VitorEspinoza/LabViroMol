using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Research.Presentation.Positions;

namespace LabViroMol.Modules.Research.IntegrationTests.Positions;

public class CreatePositionTests : PositionEndpointsTestBase
{
    public CreatePositionTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreatePositionRequest("Pesquisador Senior", "Cargo de pesquisador com experiencia avancada"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreatePositionRequest("", "Descrição válida"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class GetPositionTests : PositionEndpointsTestBase
{
    public GetPositionTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedPositionAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenPositionExists()
    {
        var positionId = await SeedPositionAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{positionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPositionDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class DeletePositionTests : PositionEndpointsTestBase
{
    public DeletePositionTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenPositionExists()
    {
        var positionId = await SeedPositionAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{positionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPositionDoesNotExist()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}