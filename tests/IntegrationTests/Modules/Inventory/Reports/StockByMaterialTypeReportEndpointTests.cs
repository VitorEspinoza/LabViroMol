using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public class StockByMaterialTypeReportEndpointTests : BaseIntegrationTest
{
    private const string Route = "/api/inventory/reports/stock/by-material-type.pdf";

    public StockByMaterialTypeReportEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturnPdf_WhenReportRequestIsAuthorized()
    {
        var type = await SeedMaterialTypeAsync();
        await SeedMaterialAsync(type, stock: 10m);

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
    public async Task ShouldSumInflowOutflowAndBalancePerMaterialType()
    {
        var typeA = await SeedMaterialTypeAsync();
        var typeB = await SeedMaterialTypeAsync();

        var materialAId = await SeedMaterialAsync(typeA, stock: 50m);
        await SeedMaterialAsync(typeB, stock: 30m);

        await AddStockAsync(materialAId, 20m, "Entrada excepcional de material tipo A para teste");
        await WriteOffAsync(materialAId, 10m, "Saida excepcional de material tipo A para teste");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var report = await queries.GetStockByMaterialTypeAsync(
            new StockByMaterialTypeFilter(null, null),
            CancellationToken.None);

        var rowA = Assert.Single(report.Rows, r => r.MaterialTypeId == typeA.Id.Value);
        Assert.Equal(70m, rowA.InflowQuantity);
        Assert.Equal(10m, rowA.OutflowQuantity);
        Assert.Equal(60m, rowA.NetQuantity);
        Assert.Equal(60m, rowA.CurrentStockQuantity);

        var rowB = Assert.Single(report.Rows, r => r.MaterialTypeId == typeB.Id.Value);
        Assert.Equal(30m, rowB.InflowQuantity);
        Assert.Equal(0m, rowB.OutflowQuantity);
        Assert.Equal(30m, rowB.CurrentStockQuantity);
    }

    private async Task<MaterialType> SeedMaterialTypeAsync()
    {
        var type = MaterialType.Create($"Tipo {Guid.NewGuid():N}");
        await DbContext.MaterialTypes.AddAsync(type);
        await DbContext.SaveChangesAsync();
        return type;
    }

    private async Task<Guid> SeedMaterialAsync(MaterialType type, decimal stock)
    {
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

    private async Task AddStockAsync(Guid materialId, decimal quantity, string reason)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/add-stock",
            new AddStockMaterialRequest(quantity, reason));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task WriteOffAsync(Guid materialId, decimal quantity, string reason)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(quantity, null, reason));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static void AssertPdfHeader(byte[] body)
    {
        Assert.True(body.Length >= 4);
        Assert.True(body.AsSpan(0, 4).SequenceEqual("%PDF"u8));
    }
}
