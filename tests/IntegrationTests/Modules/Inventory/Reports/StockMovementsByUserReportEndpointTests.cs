using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public class StockMovementsByUserReportEndpointTests : BaseIntegrationTest
{
    private const string Route = "/api/inventory/reports/stock-movements/by-user.pdf";

    public StockMovementsByUserReportEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturnPdf_WhenReportRequestIsAuthorized()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 100m);
        await WriteOffAsync(materialId, 10m, null, "Ajuste manual de estoque para teste de relatorio");

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
    public async Task ShouldGroupByUserAndTransactionType_WhenFilteringByTransactionType()
    {
        var userId = Guid.NewGuid();
        AuthenticateAs(
            [Permissions.Inventory.StockView, Permissions.Inventory.StockManage],
            userId: userId);

        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 100m);
        await WriteOffAsync(materialId, 10m, Guid.NewGuid(), null);
        await WriteOffAsync(materialId, 5m, null, "Ajuste manual de excecao para teste do relatorio");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var filtered = await queries.GetStockMovementsByUserAsync(
            new StockMovementsByUserFilter(null, null, null, "ProjectConsumption"),
            CancellationToken.None);

        var row = Assert.Single(filtered.Rows, r => r.UserId == userId);
        Assert.Equal("ProjectConsumption", row.TransactionType);
        Assert.Equal(10m, row.TotalQuantity);

        var unfiltered = await queries.GetStockMovementsByUserAsync(
            new StockMovementsByUserFilter(null, null, null, null),
            CancellationToken.None);

        Assert.Equal(2, unfiltered.Rows.Count(r => r.UserId == userId));
    }

    private async Task WriteOffAsync(Guid materialId, decimal quantity, Guid? projectId, string? reason)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(quantity, projectId, reason));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static void AssertPdfHeader(byte[] body)
    {
        Assert.True(body.Length >= 4);
        Assert.True(body.AsSpan(0, 4).SequenceEqual("%PDF"u8));
    }
}
