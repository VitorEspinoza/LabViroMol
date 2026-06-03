using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;
using LabViroMol.Modules.Inventory.IntegrationTests;
using Xunit;

namespace LabViroMol.Modules.Inventory.IntegrationTests.MaterialTypes;

public class CreateMaterialTypeTests : MaterialTypeEndpointsTestBase
{
    public CreateMaterialTypeTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaterialTypeCommand("Tipo Válido"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateMaterialTypeCommand(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class GetMaterialTypeTests : MaterialTypeEndpointsTestBase
{
    public GetMaterialTypeTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedMaterialTypeAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenTypeExists()
    {
        var typeId = await SeedMaterialTypeAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{typeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenTypeDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ActivateMaterialTypeTests : MaterialTypeEndpointsTestBase
{
    public ActivateMaterialTypeTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenTypeExists()
    {
        var typeId = await SeedMaterialTypeAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{typeId}/activate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenTypeDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/activate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class DeactivateMaterialTypeTests : MaterialTypeEndpointsTestBase
{
    public DeactivateMaterialTypeTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenTypeExists()
    {
        var typeId = await SeedMaterialTypeAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{typeId}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenTypeDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
