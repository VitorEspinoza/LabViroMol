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
                p.Doi,
                p.PublishedOn,
                p.PublicationDate,
                p.CreatedAt,
                p.Researchers
                    .OrderBy(pr => pr.Order)
                    .Select(pr => new PublicationAuthorViewModel(
                        context.Researchers
                            .Where(r => r.Id == pr.ResearcherId)
                            .Select(r => r.Name.FullName)
                            .Single(),
                        pr.Order))
                    .ToList()))
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
                p.Researchers
                    .OrderBy(pr => pr.Order)
                    .Select(pr => new PublicationAuthorViewModel(
                        context.Researchers
                            .Where(r => r.Id == pr.ResearcherId)
                            .Select(r => r.Name.FullName)
                            .Single(),
                        pr.Order))
                    .ToList(),
                p.CreatedAt))
            .FirstOrDefaultAsync();
}
