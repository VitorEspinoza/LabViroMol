using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.IntegrationTests.Materials;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public class StockAuditEnrichmentTests : BaseIntegrationTest
{
    private const string RemovedUserFallback = "Usuario removido";
    private const string RemovedProjectFallback = "Projeto removido";

    public StockAuditEnrichmentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ManualStockAdjustments_ShouldResolveUserDisplayName_WhenUserExistsInCatalog()
    {
        var userId = Guid.NewGuid();

        using (var identityScope = Factory.Services.CreateScope())
        {
            var identityDbContext = identityScope.ServiceProvider.GetRequiredService<LabViroMolIdentityDbContext>();
            await IdentityUserTestSeeder.SeedUserAsync(identityDbContext, userId, "Maria", "Auditora");
        }

        AuthenticateAs([Permissions.Inventory.StockView, Permissions.Inventory.StockManage], userId: userId);

        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 50m);
        await WriteOffAsync(materialId, 5m, null, "Ajuste manual para teste de resolucao de nome de usuario");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var report = await queries.GetManualStockAdjustmentsAsync(
            new StockReportFilter(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null, null, null),
            CancellationToken.None);

        var row = Assert.Single(report.Rows, r => r.TransactedByUserId == userId);
        Assert.Equal("Maria Auditora", row.TransactedByUserName);
    }

    [Fact]
    public async Task ManualStockAdjustments_ShouldFallBackToRemovedUserText_WhenUserNoLongerExists()
    {
        var removedUserId = Guid.NewGuid();
        AuthenticateAs([Permissions.Inventory.StockView, Permissions.Inventory.StockManage], userId: removedUserId);

        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 50m);
        await WriteOffAsync(materialId, 5m, null, "Ajuste manual de usuario removido para teste do relatorio");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var report = await queries.GetManualStockAdjustmentsAsync(
            new StockReportFilter(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null, null, null),
            CancellationToken.None);

        var row = Assert.Single(report.Rows, r => r.TransactedByUserId == removedUserId);
        Assert.Equal(RemovedUserFallback, row.TransactedByUserName);
    }

    [Fact]
    public async Task MaterialAuditMovements_ShouldResolveProjectTitle_AndFallBackWhenProjectRemoved_AndBeNullWhenNoProject()
    {
        Guid projectId;
        using (var researchScope = Factory.Services.CreateScope())
        {
            var researchDbContext = researchScope.ServiceProvider.GetRequiredService<ResearchDbContext>();
            projectId = await ResearchProjectTestSeeder.SeedProjectAsync(researchDbContext, "Projeto Virologia Teste");
        }

        var missingProjectId = Guid.NewGuid();

        var materialId = await MaterialDataSeeder.SeedMaterialAsync(DbContext, stock: 150m);
        await WriteOffAsync(materialId, 10m, projectId, null);
        await WriteOffAsync(materialId, 5m, missingProjectId, null);
        await WriteOffAsync(materialId, 5m, null, "Baixa sem projeto vinculado para teste do relatorio");

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IStockReportQueries>();

        var report = await queries.GetMaterialAuditMovementsAsync(
            new MaterialAuditMovementsFilter(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null, null, null, null),
            CancellationToken.None);

        var resolvedRow = Assert.Single(report.Rows, r => r.ProjectId == projectId);
        Assert.Equal("Projeto Virologia Teste", resolvedRow.ProjectTitle);

        var missingRow = Assert.Single(report.Rows, r => r.ProjectId == missingProjectId);
        Assert.Equal(RemovedProjectFallback, missingRow.ProjectTitle);

        var noProjectRow = Assert.Single(
            report.Rows,
            r => r.ProjectId == null && r.Justification == "Baixa sem projeto vinculado para teste do relatorio");
        Assert.Null(noProjectRow.ProjectTitle);
    }

    private async Task WriteOffAsync(Guid materialId, decimal quantity, Guid? projectId, string? reason)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(quantity, projectId, reason));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
