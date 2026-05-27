using LabViroMol.Modules.Research.Domain.Partners;

namespace LabViroMol.Modules.Research.Infrastructure.Partners;

using LabViroMol.Modules.Research.Application.Partners.ViewModels;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PartnerQueries(ResearchDbContext context)
{
    public async Task<IReadOnlyCollection<PartnerSummaryViewModel>> GetAll()
        => await context.Partners.AsNoTracking()
            .Select(p => new PartnerSummaryViewModel(p.Id.Value, p.Name, EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .ToListAsync();

    public async Task<PartnerViewModel?> GetById(Guid id)
        => await context.Partners.AsNoTracking()
            .Where(p => p.Id == PartnerId.From(id))
            .Select(p => new PartnerViewModel(p.Id.Value, p.Name, p.Description, EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .FirstOrDefaultAsync();
}
