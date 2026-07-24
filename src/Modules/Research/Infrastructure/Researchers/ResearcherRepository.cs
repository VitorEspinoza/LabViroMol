namespace LabViroMol.Modules.Research.Infrastructure.Researchers;

using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

internal sealed class ResearcherRepository(ResearchDbContext context) : IResearcherRepository
{
    public async Task<Researcher?> GetByIdAsync(ResearcherId id, CancellationToken ct)
        => await context.Researchers.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Researcher?> GetByUserIdAsync(UserId userId, CancellationToken ct)
        => await context.Researchers.FirstOrDefaultAsync(r => EF.Property<UserId>(r, "CreatedBy") == userId, ct);

    public async Task AddAsync(Researcher researcher, CancellationToken ct)
        => await context.Researchers.AddAsync(researcher, ct);

    public void Delete(Researcher researcher)
        => context.Researchers.Remove(researcher);
}
