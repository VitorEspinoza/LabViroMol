using System.Diagnostics;

namespace LabViroMol.Modules.Research.Infrastructure.Positions;

using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PositionRepository(ResearchDbContext context) : IPositionRepository
{
    public async Task<Position?> GetByIdAsync(PositionId id, CancellationToken ct)
        => await context.Positions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Position position, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        await context.Positions.AddAsync(position, ct);

        Console.WriteLine(
            $"REAL EF ADD: {sw.ElapsedMilliseconds}ms");
    }

    public void Delete(Position position)
        => context.Positions.Remove(position);
}
