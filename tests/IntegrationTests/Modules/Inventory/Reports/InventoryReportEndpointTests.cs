using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public class InventoryReportEndpointTests : BaseIntegrationTest
{
    private const string BaseRoute = "/api/inventory/reports";
    private static readonly DateTime From = DateTime.UtcNow.AddDays(-1);
    private static readonly DateTime To = DateTime.UtcNow.AddDays(1);

    public InventoryReportEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturnPdf_WhenReportRequestIsAuthorized()
    {
        await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var response = await Client.GetAsync($"{BaseRoute}/stock-outflows/totals.pdf?from={Uri.EscapeDataString(From.ToString("O"))}&to={Uri.EscapeDataString(To.ToString("O"))}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsByteArrayAsync();
        AssertPdfHeader(body);
    }

    [Fact]
    public async Task ShouldReturnPdf_WhenReportRequestUsesDateOnlyQueryParameters()
    {
        await MaterialDataSeeder.SeedMaterialAsync(DbContext);

        var from = DateTime.UtcNow.AddDays(-365).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await Client.GetAsync($"{BaseRoute}/stock-outflows/totals.pdf?from={from}&to={to}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        AssertPdfHeader(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ShouldReturn401_WhenReportRequestIsUnauthenticated()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"{BaseRoute}/stock-outflows/totals.pdf?from={Uri.EscapeDataString(From.ToString("O"))}&to={Uri.EscapeDataString(To.ToString("O"))}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn403_WhenUserCannotViewStockReports()
    {
        AuthenticateAs([Permissions.Inventory.MaterialsView]);

        var response = await Client.GetAsync($"{BaseRoute}/stock-outflows/totals.pdf?from={Uri.EscapeDataString(From.ToString("O"))}&to={Uri.EscapeDataString(To.ToString("O"))}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ShouldNotIncludeExceptionOut_WhenGeneratingStockOutflowsByProject()
    {
        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 100m);
        var projectId = Guid.NewGuid();

        await WriteOffAsync(materialId, 30m, projectId, null);
        await WriteOffAsync(materialId, 20m, null, "Baixa excepcional para teste");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();
        var report = await queries.GetStockOutflowsByProjectAsync(
            new StockReportFilter(From, To, materialId, null, null),
            CancellationToken.None);

        var row = Assert.Single(report.Rows);
        Assert.Equal(projectId, row.ProjectId);
        Assert.Equal(30m, row.TotalQuantity);
        Assert.Equal(1, row.MovementsCount);
    }

    [Fact]
    public async Task ShouldReturnPdfAndOnlyCriticalRows_WhenGeneratingCriticalStockBalance()
    {
        var criticalMaterialId = await SeedMaterialAsync("Material Critico", stock: 5m, minStock: 10m);
        var healthyMaterialId = await SeedMaterialAsync("Material Saudavel", stock: 50m, minStock: 10m);

        var response = await Client.GetAsync($"{BaseRoute}/critical-stock-balance.pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        AssertPdfHeader(await response.Content.ReadAsByteArrayAsync());

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();
        var report = await queries.GetCriticalStockBalanceAsync(
            new CriticalStockBalanceFilter(null, null, true),
            CancellationToken.None);

        Assert.Contains(report.Rows, row => row.MaterialId == criticalMaterialId);
        Assert.DoesNotContain(report.Rows, row => row.MaterialId == healthyMaterialId);
    }

    private async Task WriteOffAsync(Guid materialId, decimal quantity, Guid? projectId, string? reason)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(quantity, projectId, reason));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<Guid> SeedMaterialAsync(string name, decimal stock, decimal minStock)
    {
        var type = MaterialType.Create($"Tipo Relatorio {Guid.NewGuid():N}");
        await DbContext.MaterialTypes.AddAsync(type);

        var material = Material.Create(
            name,
            "Sala Relatorios",
            (Quantity)minStock,
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


