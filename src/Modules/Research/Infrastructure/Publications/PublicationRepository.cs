namespace LabViroMol.Modules.Research.Infrastructure.Publications;

using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PublicationRepository(ResearchDbContext context) : IPublicationRepository
{
    public async Task<Publication?> GetByIdAsync(PublicationId id, CancellationToken ct)
        => await context.Publications
            .Include(p => p.Researchers)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Publication publication, CancellationToken ct)
        => await context.Publications.AddAsync(publication, ct);

    public void Delete(Publication publication)
        => context.Publications.Remove(publication);
    
    public async Task<List<Publication>> GetMissingEnglishTranslationAsync(int limit,
        CancellationToken ct)
    {
        var publications = await context.Publications
            .Take(limit)
            .ToListAsync(ct);

        return publications
            .Where(x =>
                !x.Translations.TryGetValue("en", out var translation)
                || string.IsNullOrWhiteSpace(translation.Title)
                || string.IsNullOrWhiteSpace(translation.Description))
            .ToList();
    }
}
