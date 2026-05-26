namespace LabViroMol.Modules.Research.Infrastructure.Partners;

using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PartnerRepository(ResearchDbContext context) : IPartnerRepository
{
    public async Task<Partner?> GetByIdAsync(PartnerId id, CancellationToken ct)
        => await context.Partners.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Partner partner, CancellationToken ct)
        => await context.Partners.AddAsync(partner, ct);

    public void Delete(Partner partner)
        => context.Partners.Remove(partner);
}
