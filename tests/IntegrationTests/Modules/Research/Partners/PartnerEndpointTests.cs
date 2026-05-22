using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Research.Presentation.Partners;

namespace LabViroMol.Modules.Research.IntegrationTests.Partners;

public class CreatePartnerTests : PartnerEndpointsTestBase
{
    public CreatePartnerTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreatePartnerRequest("Instituto de Pesquisa Válido", "Descrição válida"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreatePartnerRequest("", "Descrição válida"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class GetPartnerTests : PartnerEndpointsTestBase
{
    public GetPartnerTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedPartnerAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenPartnerExists()
    {
        var partnerId = await SeedPartnerAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{partnerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPartnerDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class UpdatePartnerTests : PartnerEndpointsTestBase
{
    public UpdatePartnerTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var partnerId = await SeedPartnerAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{partnerId}",
            new UpdatePartnerRequest("Instituto Atualizado", "Nova descrição"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPartnerDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}",
            new UpdatePartnerRequest("Instituto Atualizado", "Nova descrição"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsTooShort()
    {
        var partnerId = await SeedPartnerAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{partnerId}",
            new UpdatePartnerRequest("ab", "Descrição válida"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class DeletePartnerTests : PartnerEndpointsTestBase
{
    public DeletePartnerTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenPartnerExists()
    {
        var partnerId = await SeedPartnerAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{partnerId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPartnerDoesNotExist()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}