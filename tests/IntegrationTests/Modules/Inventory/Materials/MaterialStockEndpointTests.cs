using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.IntegrationTests;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Materials;

public class AddStockTests : MaterialEndpointsTestBase
{
    public AddStockTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204AndIncrementStock_WhenRequestIsValid()
    {
        var materialId = await SeedMaterialAsync(stock: 50);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/add-stock",
            new AddStockMaterialRequest(25m, "Justificativa de adição manual de estoque"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await DbContext.Materials
            .AsNoTracking()
            .FirstAsync(m => m.Id == MaterialId.From(materialId));
        Assert.Equal(75m, updated.StockQuantity.Value);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/add-stock",
            new AddStockMaterialRequest(10m, "Justificativa válida de adição"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenQuantityIsZero()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/add-stock",
            new AddStockMaterialRequest(0m, "Justificativa válida de adição"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenReasonIsEmpty()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/add-stock",
            new AddStockMaterialRequest(10m, ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenReasonIsTooShort()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/add-stock",
            new AddStockMaterialRequest(10m, "curta"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class WriteOffWithProjectTests : MaterialEndpointsTestBase
{
    public WriteOffWithProjectTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204AndDecrementStock_WhenConsumedForProject()
    {
        var materialId = await SeedMaterialAsync(stock: 100);
        var projectId = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(30m, projectId, null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await DbContext.Materials
            .AsNoTracking()
            .FirstAsync(m => m.Id == MaterialId.From(materialId));
        Assert.Equal(70m, updated.StockQuantity.Value);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/write-off",
            new WriteOffRequest(10m, Guid.NewGuid(), null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenInsufficientStock()
    {
        var materialId = await SeedMaterialAsync(stock: 10);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(50m, Guid.NewGuid(), null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenQuantityIsZero()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(0m, Guid.NewGuid(), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class WriteOffWithoutProjectTests : MaterialEndpointsTestBase
{
    public WriteOffWithoutProjectTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204AndDecrementStock_WhenRemovedAsException()
    {
        var materialId = await SeedMaterialAsync(stock: 100);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(20m, null, "Justificativa de remoção de estoque"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await DbContext.Materials
            .AsNoTracking()
            .FirstAsync(m => m.Id == MaterialId.From(materialId));
        Assert.Equal(80m, updated.StockQuantity.Value);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/write-off",
            new WriteOffRequest(10m, null, "Justificativa de remoção de estoque"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenInsufficientStock()
    {
        var materialId = await SeedMaterialAsync(stock: 5);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(50m, null, "Justificativa de remoção de estoque"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenQuantityIsZero()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(0m, null, "Justificativa de remoção de estoque"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenReasonIsTooShort()
    {
        var materialId = await SeedMaterialAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{materialId}/write-off",
            new WriteOffRequest(10m, null, "curta"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
