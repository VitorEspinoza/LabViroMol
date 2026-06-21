using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Assets.Application.Equipments.ViewModels;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.IntegrationTests;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Assets.IntegrationTests.Equipments;

public class GetInstitutionalEquipmentsTests : EquipmentEndpointsTestBase
{
    private const string PublicRoute = "/api/assets/public/equipments";

    public GetInstitutionalEquipmentsTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenListingAnonymously()
    {
        await SeedEquipmentAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<EquipmentViewModel>>();
        Assert.NotEmpty(page!.Data);
    }

    [Fact]
    public async Task ShouldReturn200_WhenGettingByIdAnonymously()
    {
        var equipmentId = await SeedEquipmentAsync();
        ClearAuthentication();

        var response = await Client.GetAsync($"{PublicRoute}/{equipmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var equipment = await response.Content.ReadFromJsonAsync<EquipmentViewModel>();
        Assert.Equal(equipmentId, equipment!.Id);
    }

    [Fact]
    public async Task ShouldReturn404_WhenEquipmentDoesNotExist()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"{PublicRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnTranslatedName_WhenLanguageHasTranslation()
    {
        var equipmentId = await SeedEquipmentAsync();
        var equipment = await DbContext.Equipments.FindAsync(EquipmentId.From(equipmentId));
        equipment!.AddTranslation("en", "Electron Microscope", "Microscope for sample analysis");
        await DbContext.SaveChangesAsync();
        ClearAuthentication();

        var response = await Client.GetAsync($"{PublicRoute}/{equipmentId}?language=en");
        var result = await response.Content.ReadFromJsonAsync<EquipmentViewModel>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Electron Microscope", result!.Name);
    }
}

public class GetSchedulableEquipmentsTests : EquipmentEndpointsTestBase
{
    private const string SchedulableRoute = "/api/assets/public/equipments/schedulable";

    public GetSchedulableEquipmentsTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenCalledAnonymously()
    {
        await SeedEquipmentAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(SchedulableRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var equipments = await response.Content.ReadFromJsonAsync<List<EquipmentSchedulableViewModel>>();
        Assert.NotEmpty(equipments!);
    }

    [Fact]
    public async Task ShouldExcludeEquipment_WhenItHasActiveMaintenanceRequest()
    {
        var equipmentId = await SeedEquipmentAsync();
        var maintenanceRequest = MaintenanceRequest.Create(
            "Manutencao preventiva", "Ruido anormal", equipmentId).Data!;
        await DbContext.MaintenanceRequests.AddAsync(maintenanceRequest);
        await DbContext.SaveChangesAsync();
        ClearAuthentication();

        var response = await Client.GetAsync(SchedulableRoute);
        var equipments = await response.Content.ReadFromJsonAsync<List<EquipmentSchedulableViewModel>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(equipments!, e => e.Id == equipmentId);
    }
}
