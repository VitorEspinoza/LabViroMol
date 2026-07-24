using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Inventory.IntegrationTests.Orders;
using LabViroMol.Modules.Inventory.Presentation.Orders;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public class OrderStatusCycleReportEndpointTests : BaseIntegrationTest
{
    private const string Route = "/api/inventory/reports/orders/status-cycle.pdf";
    private const string OrdersRoute = "/api/inventory/orders";

    public OrderStatusCycleReportEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturnPdf_WhenReportRequestIsAuthorized()
    {
        await OrderDataSeeder.SeedPendingOrderAsync(DbContext);

        var response = await Client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        AssertPdfHeader(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ShouldReturn401_WhenUnauthenticated()
    {
        ClearAuthentication();

        var response = await Client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn403_WhenUserCannotViewStockReports()
    {
        AuthenticateAs([Permissions.Inventory.MaterialsView]);

        var response = await Client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ShouldCountOrdersByStatusAndFlagOnlyOrdersStaleBeyondStaleDays()
    {
        var staleOrderId = await SeedOrderAsync();
        var recentOrderId = await SeedOrderAsync();
        var completedOrderId = await SeedOrderAsync();

        await Client.PostAsJsonAsync($"{OrdersRoute}/{completedOrderId}/process", new ProcessOrderRequest(null));
        await Client.PostAsJsonAsync($"{OrdersRoute}/{completedOrderId}/receive", new ReceiveOrderRequest(5m, null));

        await DbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE inventory."InventoryOrders" SET "CreatedAt" = {DateTimeOffset.UtcNow.AddDays(-30)} WHERE "Id" = {staleOrderId}""");
        await DbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE inventory."InventoryOrders" SET "CreatedAt" = {DateTimeOffset.UtcNow} WHERE "Id" = {recentOrderId}""");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var report = await queries.GetOrderStatusCycleAsync(
            new OrderStatusCycleFilter(null, null, StaleDays: 15),
            CancellationToken.None);

        Assert.Contains(report.StatusCounts, s => s.Status == "Pending" && s.Count >= 2);
        Assert.Contains(report.StatusCounts, s => s.Status == "Completed" && s.Count >= 1);
        Assert.Contains(report.StaleOrders, o => o.OrderId == staleOrderId);
        Assert.DoesNotContain(report.StaleOrders, o => o.OrderId == recentOrderId);
        Assert.DoesNotContain(report.StaleOrders, o => o.OrderId == completedOrderId);
    }

    private async Task<Guid> SeedOrderAsync()
    {
        var type = MaterialType.Create($"Tipo {Guid.NewGuid():N}");
        await DbContext.MaterialTypes.AddAsync(type);

        var material = Material.Create(
            $"Material {Guid.NewGuid():N}",
            "Sala Relatorios",
            (Quantity)10m,
            (Quantity)100m,
            Unit.Gram,
            type).Data!;
        await DbContext.Materials.AddAsync(material);

        var order = Order.Create(
            material.Id,
            ProjectId.From(Guid.NewGuid()),
            (Quantity)10m,
            "Pedido de teste para relatorio de ciclo de status");
        await DbContext.Orders.AddAsync(order);

        await DbContext.SaveChangesAsync();

        return order.Id.Value;
    }

    private static void AssertPdfHeader(byte[] body)
    {
        Assert.True(body.Length >= 4);
        Assert.True(body.AsSpan(0, 4).SequenceEqual("%PDF"u8));
    }
}
