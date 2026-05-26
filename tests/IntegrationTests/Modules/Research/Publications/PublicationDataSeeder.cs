using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.IntegrationTests.Publications;

public static class PublicationDataSeeder
{
    public static async Task<Guid> SeedPublicationAsync(ResearchDbContext dbContext)
    {
        var publication = Publication.Create(
            "Estudo de Virologia Molecular em Amostras Clinicas",
            "Descricao detalhada do estudo de virologia molecular",
            "10.1234/test",
            new DateOnly(2024, 1, 1),
            "Nature Virology",
            "https://example.com/pub").Data!;

        await dbContext.Publications.AddAsync(publication);
        await dbContext.SaveChangesAsync();

        return publication.Id.Value;
    }
}
