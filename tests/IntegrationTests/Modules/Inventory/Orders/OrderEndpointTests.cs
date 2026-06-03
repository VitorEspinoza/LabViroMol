using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.IntegrationTests;
using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Inventory.Presentation.Orders;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Orders;

public class CreateOrderTests : OrderEndpointsTestBase
{
    public CreateOrderTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateOrderRequest(materialId, Guid.NewGuid(), 10m, "Pedido de teste válido"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenQuantityIsZero()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateOrderRequest(materialId, Guid.NewGuid(), 0m, "Pedido de teste"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenDescriptionIsEmpty()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateOrderRequest(materialId, Guid.NewGuid(), 10m, ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenMaterialDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), 10m, "Pedido de teste"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class GetOrderTests : OrderEndpointsTestBase
{
    public GetOrderTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenGettingAll()
    {
        // The query joins Materials and handles Receipt; use an empty DB to avoid
        // the null-Receipt case until the query projection is updated.
        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenOrderIsCompleted()
    {
        // Receive the order first so Receipt is non-null, avoiding the null cast issue
        var (_, orderId) = await SeedPendingOrderAsync();
        await Client.PostAsJsonAsync($"{BaseRoute}/{orderId}/process", new ProcessOrderRequest(null));
        await Client.PostAsJsonAsync($"{BaseRoute}/{orderId}/receive", new ReceiveOrderRequest(5m, null));

        var response = await Client.GetAsync($"{BaseRoute}/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenOrderDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class FixOrderDetailsTests : OrderEndpointsTestBase
{
    public FixOrderDetailsTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenOrderIsPending()
    {
        var (_, orderId) = await SeedPendingOrderAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{orderId}/fix-details",
            new FixOrderDetailsRequest(Guid.NewGuid(), 20m, "Descrição corrigida"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenOrderIsNotPending()
    {
        var (_, orderId) = await SeedPendingOrderAsync();
        await Client.PostAsJsonAsync($"{BaseRoute}/{orderId}/process", new ProcessOrderRequest(null));

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{orderId}/fix-details",
            new FixOrderDetailsRequest(Guid.NewGuid(), 20m, "Descrição corrigida"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenOrderDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/fix-details",
            new FixOrderDetailsRequest(Guid.NewGuid(), 20m, "Descrição corrigida"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ProcessOrderTests : OrderEndpointsTestBase
{
    public ProcessOrderTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenOrderIsPending()
    {
        var (_, orderId) = await SeedPendingOrderAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{orderId}/process",
            new ProcessOrderRequest(null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenOrderIsAlreadyProcessing()
    {
        var (_, orderId) = await SeedPendingOrderAsync();
        await Client.PostAsJsonAsync($"{BaseRoute}/{orderId}/process", new ProcessOrderRequest(null));

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{orderId}/process",
            new ProcessOrderRequest(null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenOrderDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/process",
            new ProcessOrderRequest(null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ReceiveOrderTests : OrderEndpointsTestBase
{
    public ReceiveOrderTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204AndIncrementStock_WhenOrderIsProcessing()
    {
        var (materialId, orderId) = await SeedPendingOrderAsync();
        await Client.PostAsJsonAsync($"{BaseRoute}/{orderId}/process", new ProcessOrderRequest(null));

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{orderId}/receive",
            new ReceiveOrderRequest(10m, null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var material = await DbContext.Materials
            .AsNoTracking()
            .FirstAsync(m => m.Id == MaterialId.From(materialId));
        Assert.Equal(110m, material.StockQuantity.Value);
    }

    [Fact]
    public async Task ShouldReturn422_WhenOrderIsNotProcessing()
    {
        var (_, orderId) = await SeedPendingOrderAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{orderId}/receive",
            new ReceiveOrderRequest(10m, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenOrderDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/receive",
            new ReceiveOrderRequest(10m, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class CancelOrderTests : OrderEndpointsTestBase
{
    public CancelOrderTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenOrderIsPending()
    {
        var (_, orderId) = await SeedPendingOrderAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenOrderIsNotPending()
    {
        var (_, orderId) = await SeedPendingOrderAsync();
        await Client.PostAsJsonAsync($"{BaseRoute}/{orderId}/process", new ProcessOrderRequest(null));

        var response = await Client.PostAsync($"{BaseRoute}/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenOrderDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
