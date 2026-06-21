using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Research.Application.Partners.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Research.IntegrationTests.Partners;

public class GetInstitutionalPartnersTests : PartnerEndpointsTestBase
{
    private const string PublicRoute = "/api/research/public/partners";

    public GetInstitutionalPartnersTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenListingAnonymously()
    {
        await SeedPartnerAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PartnerSummaryViewModel>>();
        Assert.NotEmpty(page!.Data);
    }
}
