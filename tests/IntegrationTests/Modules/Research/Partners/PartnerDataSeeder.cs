using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.IntegrationTests.Partners;

public static class PartnerDataSeeder
{
    public static async Task<Guid> SeedPartnerAsync(ResearchDbContext dbContext)
    {
        var partner = Partner.Create(
            "Instituto de Pesquisa Teste", "Parceiro de pesquisa para testes").Data!;

        await dbContext.Partners.AddAsync(partner);
        await dbContext.SaveChangesAsync();

        return partner.Id.Value;
    }
}
