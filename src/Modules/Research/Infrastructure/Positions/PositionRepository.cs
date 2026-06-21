using System.Diagnostics;

namespace LabViroMol.Modules.Research.Infrastructure.Positions;

using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PositionRepository(ResearchDbContext context) : IPositionRepository
{
    public async Task<Position?> GetByIdAsync(PositionId id, CancellationToken ct)
        => await context.Positions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Position position, CancellationToken ct) 
        => await context.Positions.AddAsync(position, ct);

    public void Delete(Position position)
        => context.Positions.Remove(position);
    
    public async Task<List<Position>> GetMissingEnglishTranslationAsync(int limit,
        CancellationToken ct)
    {
        var positions = await context.Positions
            .Take(limit)
            .ToListAsync(ct);

        return positions
            .Where(x =>
                !x.Translations.TryGetValue("en", out var translation)
                || string.IsNullOrWhiteSpace(translation.Name)
                || string.IsNullOrWhiteSpace(translation.Description))
            .ToList();
    }
}
