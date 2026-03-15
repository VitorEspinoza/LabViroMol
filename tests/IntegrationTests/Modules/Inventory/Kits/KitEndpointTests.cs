using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Shared;
using LabViroMol.Modules.Inventory.IntegrationTests;
using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Inventory.Presentation.Kits;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Kits;

public class CreateKitTests : KitEndpointsTestBase
{
    public CreateKitTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateKitCommand("Kit Teste", "Descrição do kit", new List<KitItemInputModel>
            {
                new KitItemInputModel(materialId, 1m)
            }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateKitCommand("", "Descrição do kit", new List<KitItemInputModel>
            {
                new KitItemInputModel(materialId, 1m)
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenItemQuantityIsZero()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateKitCommand("Kit Teste", "Descrição do kit", new List<KitItemInputModel>
            {
                new KitItemInputModel(materialId, 0m)
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenMaterialsHasDuplicates()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateKitCommand("Kit Teste", "Descrição do kit", new List<KitItemInputModel>
            {
                new KitItemInputModel(materialId, 1m),
                new KitItemInputModel(materialId, 2m)
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialInKitDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateKitCommand("Kit Teste", "Descrição do kit", new List<KitItemInputModel>
            {
                new KitItemInputModel(Guid.NewGuid(), 1m)
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class GetKitTests : KitEndpointsTestBase
{
    public GetKitTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedKitAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenKitExists()
    {
        var (kitId, _) = await SeedKitAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{kitId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenKitDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class UpdateKitTests : KitEndpointsTestBase
{
    public UpdateKitTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var (kitId, materialId) = await SeedKitAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{kitId}",
            new UpdateKitRequest("Kit Atualizado", "Nova descrição", new List<KitItemInputModel>
            {
                new KitItemInputModel(materialId, 2m)
            }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenKitDoesNotExist()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}",
            new UpdateKitRequest("Kit Atualizado", "Nova descrição", new List<KitItemInputModel>
            {
                new KitItemInputModel(materialId, 1m)
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialInKitDoesNotExist()
    {
        var (kitId, _) = await SeedKitAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{kitId}",
            new UpdateKitRequest("Kit Atualizado", "Nova descrição", new List<KitItemInputModel>
            {
                new KitItemInputModel(Guid.NewGuid(), 1m)
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
