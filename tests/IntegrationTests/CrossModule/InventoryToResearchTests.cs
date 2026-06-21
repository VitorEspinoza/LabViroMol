using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Material = LabViroMol.Modules.Inventory.Domain.Materials.Material;
using Unit = LabViroMol.Modules.Inventory.Domain.Materials.Unit;

namespace LabViroMol.IntegrationTests.CrossModule;

public class InventoryToResearchTests : BaseCrossModuleTest
{
    public InventoryToResearchTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private async Task<Guid> SeedMaterialAsync()
    {
        var materialType = MaterialType.Create("Reagente de Teste");
        await InventoryDbContext.MaterialTypes.AddAsync(materialType);

        var material = Material.Create(
            "Reagente A", "Estante 1", new Quantity(5), new Quantity(50), Unit.Milliliter, materialType).Data!;
        await InventoryDbContext.Materials.AddAsync(material);
        await InventoryDbContext.SaveChangesAsync();

        return material.Id.Value;
    }

    private async Task<(Guid researcherId, Guid projectId)> SeedProjectAsync(ProjectStatus status, bool addAsMember)
    {
        var researcherId = Guid.NewGuid();

        var position = Position.Create("Pesquisador", "Cargo").Data!;
        await ResearchDbContext.Positions.AddAsync(position);

        var researcher = Researcher.Create(
            ResearcherId.From(researcherId),
            new ResearcherName("Ana", "Souza", null, null),
            null,
            new AcademicBackground(DegreeLevel.Doctorate, "Bioquímica"),
            position.Id);
        await ResearchDbContext.Researchers.AddAsync(researcher);

        var partner = Partner.Create("Parceiro de Teste", null).Data!;
        await ResearchDbContext.Partners.AddAsync(partner);

        var leadId = researcherId;
        if (!addAsMember)
        {
            leadId = Guid.NewGuid();
            var lead = Researcher.Create(
                ResearcherId.From(leadId),
                new ResearcherName("Outro", "Lider", null, null),
                null,
                new AcademicBackground(DegreeLevel.Doctorate, "Bioquímica"),
                position.Id);
            await ResearchDbContext.Researchers.AddAsync(lead);
        }

        var project = Project.Create(ResearcherId.From(leadId), "Projeto de Teste", "Descrição do projeto", partner.Id).Data!;

        if (status == ProjectStatus.InProgress)
            project.Start(ResearcherId.From(leadId));

        await ResearchDbContext.Projects.AddAsync(project);
        await ResearchDbContext.SaveChangesAsync();

        return (researcherId, project.Id.Value);
    }

    private void AuthenticateAsResearcher(Guid researcherId) =>
        AuthenticateAs([Permissions.Inventory.StockManage], researcherId);

    [Fact]
    public async Task ShouldAllowConsumption_WhenProjectIsInProgressAndUserIsActiveMember()
    {
        var materialId = await SeedMaterialAsync();
        var (researcherId, projectId) = await SeedProjectAsync(ProjectStatus.InProgress, addAsMember: true);
        AuthenticateAsResearcher(researcherId);

        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(10, projectId, null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var material = await InventoryDbContext.Materials.AsNoTracking()
            .FirstAsync(m => m.Id == Modules.Inventory.Domain.Materials.MaterialId.From(materialId));
        Assert.Equal(40, material.StockQuantity.Value);
    }

    [Fact]
    public async Task ShouldBlockConsumption_WhenProjectIsNotInProgress()
    {
        var materialId = await SeedMaterialAsync();
        var (researcherId, projectId) = await SeedProjectAsync(ProjectStatus.Planned, addAsMember: true);
        AuthenticateAsResearcher(researcherId);

        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(10, projectId, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldBlockConsumption_WhenUserIsNotAProjectMember()
    {
        var materialId = await SeedMaterialAsync();
        var (researcherId, projectId) = await SeedProjectAsync(ProjectStatus.InProgress, addAsMember: false);
        AuthenticateAsResearcher(researcherId);

        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(10, projectId, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldBlockConsumption_WhenProjectDoesNotExist()
    {
        var materialId = await SeedMaterialAsync();
        AuthenticateAsResearcher(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(
            $"/api/inventory/materials/{materialId}/write-off",
            new WriteOffRequest(10, Guid.NewGuid(), null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
