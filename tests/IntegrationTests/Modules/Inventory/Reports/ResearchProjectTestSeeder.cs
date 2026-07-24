using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public static class ResearchProjectTestSeeder
{
    public static async Task<Guid> SeedProjectAsync(ResearchDbContext dbContext, string title)
    {
        var partner = Partner.Create(
            $"Instituto Parceiro {Guid.NewGuid():N}",
            "Parceiro de pesquisa usado para testes de relatorio de estoque").Data!;
        await dbContext.Partners.AddAsync(partner);

        var position = Position.Create(
            $"Cargo Teste {Guid.NewGuid():N}",
            "Cargo de pesquisador usado para testes de relatorio de estoque").Data!;
        await dbContext.Positions.AddAsync(position);

        var researcherId = IdFactory.New<ResearcherId>();
        var researcher = Researcher.Create(
            researcherId,
            new ResearcherName("Joana", "Pesquisadora", null, null),
            null,
            new AcademicBackground(DegreeLevel.Doctorate, "Virologia"),
            position.Id);
        await dbContext.Researchers.AddAsync(researcher);

        await dbContext.SaveChangesAsync();

        var project = Project.Create(
            researcherId,
            title,
            "Descricao do projeto de teste usado para relatorios de estoque",
            partner.Id).Data!;

        await dbContext.Projects.AddAsync(project);
        await dbContext.SaveChangesAsync();

        return project.Id.Value;
    }
}
