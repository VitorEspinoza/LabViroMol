using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Research.IntegrationTests.Projects;

public class GetInstitutionalProjectsTests : ProjectEndpointsTestBase
{
    private const string PublicRoute = "/api/research/public/projects";

    public GetInstitutionalProjectsTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenListingAnonymously()
    {
        await SeedProjectAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PublicProjectViewModel>>();
        Assert.NotEmpty(page!.Data);
    }

    [Fact]
    public async Task ShouldIncludePlannedAndInProgressProjects()
    {
        await SeedProjectAsync();
        await SeedStartedProjectAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(PublicRoute);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PublicProjectViewModel>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(page!.Data, p => p.Status == "Planned");
        Assert.Contains(page.Data, p => p.Status == "InProgress");
    }
}
