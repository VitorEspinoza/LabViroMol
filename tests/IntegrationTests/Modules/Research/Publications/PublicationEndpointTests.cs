using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Research.Application.Publications.Commands.Create;
using LabViroMol.Modules.Research.IntegrationTests.Researchers;
using LabViroMol.Modules.Research.Presentation.Publications;

namespace LabViroMol.Modules.Research.IntegrationTests.Publications;

public class CreatePublicationTests : PublicationEndpointsTestBase
{
    public CreatePublicationTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreatePublicationCommand(
                "Estudo de Virologia Molecular",
                "Descrição do estudo",
                "10.1234/test",
                new DateOnly(2024, 1, 1),
                "Nature Virology",
                "https://example.com/pub"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenTitleIsEmpty()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreatePublicationCommand(
                "",
                "Descrição válida",
                "10.1234/test",
                new DateOnly(2024, 1, 1),
                "Nature Virology",
                "https://example.com/pub"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class GetPublicationTests : PublicationEndpointsTestBase
{
    public GetPublicationTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedPublicationAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenPublicationExists()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{publicationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class UpdatePublicationTests : PublicationEndpointsTestBase
{
    public UpdatePublicationTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{publicationId}",
            new UpdatePublicationRequest(
                "Título Atualizado",
                "Descrição atualizada",
                "Journal Atualizado",
                "https://example.com/updated"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}",
            new UpdatePublicationRequest(
                "Título Atualizado",
                "Descrição atualizada",
                "Journal Atualizado",
                "https://example.com/updated"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenTitleIsTooShort()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{publicationId}",
            new UpdatePublicationRequest(
                "ab",
                "Descrição válida",
                "Nature Virology",
                "https://example.com/pub"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class AssignDoiTests : PublicationEndpointsTestBase
{
    public AssignDoiTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{publicationId}/doi",
            new AssignDoiRequest("10.5678/assigned"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/doi",
            new AssignDoiRequest("10.5678/assigned"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenDoiIsEmpty()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{publicationId}/doi",
            new AssignDoiRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class AddPublicationResearcherTests : PublicationEndpointsTestBase
{
    public AddPublicationResearcherTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var publicationId = await SeedPublicationAsync();
        var (researcherId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{publicationId}/researchers",
            new AddPublicationResearcherRequest(researcherId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var (researcherId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/researchers",
            new AddPublicationResearcherRequest(researcherId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenResearcherDoesNotExist()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{publicationId}/researchers",
            new AddPublicationResearcherRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class RemovePublicationResearcherTests : PublicationEndpointsTestBase
{
    public RemovePublicationResearcherTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenResearcherExists()
    {
        var publicationId = await SeedPublicationAsync();
        var (researcherId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        await Client.PostAsJsonAsync(
            $"{BaseRoute}/{publicationId}/researchers",
            new AddPublicationResearcherRequest(researcherId));

        var response = await Client.DeleteAsync($"{BaseRoute}/{publicationId}/researchers/{researcherId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}/researchers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ReorderPublicationResearchersTests : PublicationEndpointsTestBase
{
    public ReorderPublicationResearchersTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var publicationId = await SeedPublicationAsync();
        var (r1, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);
        var (r2, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        await Client.PostAsJsonAsync($"{BaseRoute}/{publicationId}/researchers", new AddPublicationResearcherRequest(r1));
        await Client.PostAsJsonAsync($"{BaseRoute}/{publicationId}/researchers", new AddPublicationResearcherRequest(r2));

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{publicationId}/researchers/order",
            new ReorderPublicationResearchersRequest([r2, r1]));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/researchers/order",
            new ReorderPublicationResearchersRequest([Guid.NewGuid()]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class DeletePublicationTests : PublicationEndpointsTestBase
{
    public DeletePublicationTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenPublicationExists()
    {
        var publicationId = await SeedPublicationAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{publicationId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenPublicationDoesNotExist()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}