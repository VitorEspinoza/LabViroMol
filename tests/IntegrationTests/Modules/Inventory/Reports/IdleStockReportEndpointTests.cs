using System.Net;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public class IdleStockReportEndpointTests : BaseIntegrationTest
{
    private const string Route = "/api/inventory/reports/materials/idle-stock.pdf";

    public IdleStockReportEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturnPdf_WhenReportRequestIsAuthorized()
    {
        await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 10m);

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
    public async Task ShouldOnlyReturnPositiveStockMaterialsWithoutMovementSinceCutoff()
    {
        var idleMaterialId = await SeedMaterialAsync(stock: 20m);
        var recentMaterialId = await SeedMaterialAsync(stock: 20m);
        var zeroStockMaterialId = await SeedMaterialAsync(stock: 0m);

        await DbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE inventory."StockTransactions" SET "TransactedAt" = {DateTime.UtcNow.AddDays(-200)} WHERE "MaterialId" = {idleMaterialId}""");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var report = await queries.GetIdleStockAsync(
            new IdleStockFilter(null, DateTime.UtcNow.AddDays(-180)),
            CancellationToken.None);

        Assert.Contains(report.Rows, r => r.MaterialId == idleMaterialId);
        Assert.DoesNotContain(report.Rows, r => r.MaterialId == recentMaterialId);
        Assert.DoesNotContain(report.Rows, r => r.MaterialId == zeroStockMaterialId);
    }

    private async Task<Guid> SeedMaterialAsync(decimal stock)
    {
        var type = MaterialType.Create($"Tipo {Guid.NewGuid():N}");
        await DbContext.MaterialTypes.AddAsync(type);

        var material = Material.Create(
            $"Material {Guid.NewGuid():N}",
            "Sala Relatorios",
            (Quantity)10m,
            (Quantity)stock,
            Unit.Gram,
            type).Data!;

        await DbContext.Materials.AddAsync(material);
        await DbContext.SaveChangesAsync();

        return material.Id.Value;
    }

    private static void AssertPdfHeader(byte[] body)
    {
        Assert.True(body.Length >= 4);
        Assert.True(body.AsSpan(0, 4).SequenceEqual("%PDF"u8));
    }
}
