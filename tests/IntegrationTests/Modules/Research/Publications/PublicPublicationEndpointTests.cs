using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Research.Application.Publications.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Research.IntegrationTests.Publications;

public class GetInstitutionalPublicationsTests : PublicationEndpointsTestBase
{
    private const string PublicRoute = "/api/research/public/publications";

    public GetInstitutionalPublicationsTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenListingAnonymously()
    {
        await SeedPublicationAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PublicationSummaryViewModel>>();
        Assert.NotEmpty(page!.Data);
    }
}
