using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.IntegrationTests;
using LabViroMol.Modules.Assets.Presentation.Equipments;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.IntegrationTests.Equipments;

public class CreateEquipmentTests : EquipmentEndpointsTestBase
{
    public CreateEquipmentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateEquipmentCommand("Microscópio", "Zeiss", "EVO 10", "EQ-001", "Microscópio eletrônico"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateEquipmentCommand("", "Zeiss", "EVO 10", "EQ-002", "Microscópio eletrônico"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenCodeAlreadyExists()
    {
        var existingCode = "EQ-DUPLICATE";
        await SeedEquipmentAsync(code: existingCode);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateEquipmentCommand("Microscópio", "Zeiss", "EVO 10", existingCode, "Microscópio eletrônico"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn403_WhenUserHasNoManagePermission()
    {
        AuthenticateAs([Permissions.Assets.EquipmentsView]);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateEquipmentCommand("Microscópio", "Zeiss", "EVO 10", "EQ-003", "Microscópio eletrônico"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        ClearAuthentication();

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateEquipmentCommand("Microscópio", "Zeiss", "EVO 10", "EQ-004", "Microscópio eletrônico"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class UpdateEquipmentTests : EquipmentEndpointsTestBase
{
    public UpdateEquipmentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenRequestIsValid()
    {
        var equipmentId = await SeedEquipmentAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{equipmentId}",
            new UpdateEquipmentRequest("Novo Nome", "Nova Marca", "Novo Modelo", "Nova descrição", null));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var equipment = await DbContext.Equipments
            .AsNoTracking()
            .FirstAsync(e => e.Id == EquipmentId.From(equipmentId));
        Assert.Equal("Novo Nome", equipment.Name);
    }

    [Fact]
    public async Task ShouldReturn422_WhenEquipmentDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}",
            new UpdateEquipmentRequest("Novo Nome", "Nova Marca", "Novo Modelo", "Nova descrição", null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class DeleteEquipmentTests : EquipmentEndpointsTestBase
{
    public DeleteEquipmentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenEquipmentExists()
    {
        var equipmentId = await SeedEquipmentAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{equipmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var stillVisible = await DbContext.Equipments
            .AsNoTracking()
            .AnyAsync(e => e.Id == EquipmentId.From(equipmentId));
        Assert.False(stillVisible);
    }

    [Fact]
    public async Task ShouldReturn204_WhenEquipmentDoesNotExist()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

public class GetEquipmentTests : EquipmentEndpointsTestBase
{
    public GetEquipmentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenGettingAll()
    {
        await SeedEquipmentAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenEquipmentExists()
    {
        var equipmentId = await SeedEquipmentAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{equipmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenEquipmentDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
