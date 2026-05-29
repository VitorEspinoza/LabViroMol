namespace LabViroMol.Modules.Research.Infrastructure.Publications;

using LabViroMol.Modules.Research.Application.Publications.ViewModels;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PublicationQueries(ResearchDbContext context)
{
    public async Task<IReadOnlyCollection<PublicationSummaryViewModel>> GetAll()
        => await context.Publications.AsNoTracking()
            .Select(p => new PublicationSummaryViewModel(
                p.Id.Value,
                p.Title,
                p.PublishedOn,
                p.PublicationDate,
                EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .ToListAsync();

    public async Task<PublicationViewModel?> GetById(Guid id)
        => await context.Publications.AsNoTracking()
            .Include(p => p.Researchers)
            .Where(p => p.Id == PublicationId.From(id))
            .Select(p => new PublicationViewModel(
                p.Id.Value,
                p.Title,
                p.Description,
                p.Doi,
                p.PublicationDate,
                p.PublishedOn,
                p.PublishUrl,
                p.Researchers.Select(r => r.ResearcherId.Value).ToList(),
                EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .FirstOrDefaultAsync();
}
