using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Infrastructure.Positions;

using LabViroMol.Modules.Research.Application.Positions.ViewModels;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class PositionQueries(ResearchDbContext context)
{
    public async Task<IReadOnlyCollection<PositionViewModel>> GetAll()
        => await context.Positions.AsNoTracking()
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .ToListAsync();

    public async Task<PositionViewModel?> GetById(Guid id)
        => await context.Positions.AsNoTracking()
            .Where(p => p.Id == PositionId.From(id))
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .FirstOrDefaultAsync();
}
