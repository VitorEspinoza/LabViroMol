using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.IntegrationTests;
using LabViroMol.Modules.Inventory.IntegrationTests.MaterialTypes;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using Unit = LabViroMol.Modules.Inventory.Domain.Materials.Unit;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Materials;

public class CreateMaterialTests : MaterialEndpointsTestBase
{
    public CreateMaterialTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var typeId = await MaterialTypeDataSeeder.SeedMaterialTypeAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaterialRequest("Material Teste", "Sala A", 10m, 50m, Unit.Gram, typeId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var typeId = await MaterialTypeDataSeeder.SeedMaterialTypeAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaterialRequest("", "Sala A", 10m, 50m, Unit.Gram, typeId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenTypeDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaterialRequest("Material Teste", "Sala A", 10m, 50m, Unit.Gram, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenTypeIsInactive()
    {
        var typeId = await MaterialTypeDataSeeder.SeedMaterialTypeAsync(DbContext);
        await Client.PostAsync($"/api/inventory/types/{typeId}/deactivate", null);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaterialRequest("Material Teste", "Sala A", 10m, 50m, Unit.Gram, typeId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class GetMaterialTests : MaterialEndpointsTestBase
{
    public GetMaterialTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedMaterialAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenMaterialExists()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{materialId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class UpdateMaterialTests : MaterialEndpointsTestBase
{
    public UpdateMaterialTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{materialId}",
            new UpdateMaterialRequest("Nome Atualizado", "Sala B", 5m));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}",
            new UpdateMaterialRequest("Nome Atualizado", "Sala B", 5m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{materialId}",
            new UpdateMaterialRequest("", "Sala B", 5m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
