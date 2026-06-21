using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;
using LabViroMol.Modules.Assets.IntegrationTests;
using LabViroMol.Modules.Assets.IntegrationTests.Equipments;

namespace LabViroMol.Modules.Assets.IntegrationTests.MaintenanceRequests;

public class CreateMaintenanceRequestTests : MaintenanceRequestEndpointsTestBase
{
    public CreateMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var equipmentId = await EquipmentDataSeeder.SeedEquipmentAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaintenanceCommand(equipmentId, "Manutenção preventiva", "Ruído anormal"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenDescriptionIsEmpty()
    {
        var equipmentId = await EquipmentDataSeeder.SeedEquipmentAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaintenanceCommand(equipmentId, "", "Ruído anormal"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenEquipmentDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaintenanceCommand(Guid.NewGuid(), "Manutenção preventiva", "Ruído anormal"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenEquipmentAlreadyHasActiveRequest()
    {
        var (equipmentId, _) = await SeedRequestedAsync();

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaintenanceCommand(equipmentId, "Outra manutenção", "Outro problema"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class GetMaintenanceRequestTests : MaintenanceRequestEndpointsTestBase
{
    public GetMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenGettingAll()
    {
        await SeedRequestedAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class StartMaintenanceRequestTests : MaintenanceRequestEndpointsTestBase
{
    public StartMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenRequestIsRequested()
    {
        var (_, maintenanceRequestId) = await SeedRequestedAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/start", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenRequestIsAlreadyInProgress()
    {
        var (_, maintenanceRequestId) = await SeedRequestedAsync();
        await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/start", null);

        var response = await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/start", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenRequestDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class DoneMaintenanceRequestTests : MaintenanceRequestEndpointsTestBase
{
    public DoneMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenRequestIsInProgress()
    {
        var (_, maintenanceRequestId) = await SeedRequestedAsync();
        await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/start", null);

        var response = await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/done", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenRequestIsStillRequested()
    {
        var (_, maintenanceRequestId) = await SeedRequestedAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/done", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenRequestDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/done", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class CancelMaintenanceRequestTests : MaintenanceRequestEndpointsTestBase
{
    public CancelMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenRequestIsNotDone()
    {
        var (_, maintenanceRequestId) = await SeedRequestedAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/cancel", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenRequestIsAlreadyDone()
    {
        var (_, maintenanceRequestId) = await SeedRequestedAsync();
        await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/start", null);
        await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/done", null);

        var response = await Client.PostAsync($"{BaseRoute}/{maintenanceRequestId}/cancel", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenRequestDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
