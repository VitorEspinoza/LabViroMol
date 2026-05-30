using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Research.IntegrationTests.Partners;
using LabViroMol.Modules.Research.IntegrationTests.Researchers;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.IntegrationTests.Projects;

public static class ProjectDataSeeder
{
    public static async Task<(Guid projectId, Guid leadResearcherId, Guid partnerId)> SeedProjectAsync(
        ResearchDbContext dbContext)
    {
        var partnerId = await PartnerDataSeeder.SeedPartnerAsync(dbContext);
        var (researcherId, _) = await ResearcherDataSeeder.SeedResearcherAsync(dbContext);

        var project = Project.Create(
            ResearcherId.From(researcherId),
            "Projeto de Pesquisa Virologica",
            "Descricao detalhada do projeto de pesquisa virologica",
            PartnerId.From(partnerId)).Data!;

        await dbContext.Projects.AddAsync(project);
        await dbContext.SaveChangesAsync();

        return (project.Id.Value, researcherId, partnerId);
    }

    public static async Task<(Guid projectId, Guid leadResearcherId, Guid partnerId)> SeedStartedProjectAsync(
        ResearchDbContext dbContext)
    {
        var (projectId, leadId, partnerId) = await SeedProjectAsync(dbContext);

        var project = await dbContext.Projects.FindAsync(ProjectId.From(projectId));
        project!.Start(ResearcherId.From(leadId));
        await dbContext.SaveChangesAsync();

        return (projectId, leadId, partnerId);
    }
}
