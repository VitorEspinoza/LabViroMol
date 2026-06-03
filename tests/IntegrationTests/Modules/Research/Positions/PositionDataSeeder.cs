using System;
using System.Threading.Tasks;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.IntegrationTests.Positions;

public static class PositionDataSeeder
{
    public static async Task<Guid> SeedPositionAsync(ResearchDbContext dbContext)
    {
        var position = Position.Create(
            "Pesquisador Senior", "Cargo de pesquisador com experiencia avancada").Data!;

        await dbContext.Positions.AddAsync(position);
        await dbContext.SaveChangesAsync();

        return position.Id.Value;
    }
}
