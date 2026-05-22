namespace LabViroMol.Modules.Research.Infrastructure.Researchers;

using LabViroMol.Modules.Research.Application.Researchers.ViewModels;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ResearcherQueries(ResearchDbContext context)
{
    public async Task<IReadOnlyCollection<ResearcherSummaryViewModel>> GetAll()
        => await context.Researchers.AsNoTracking()
            .Join(context.Positions,
                r => r.PositionId,
                p => p.Id, 
                (r, p) => 
                     new ResearcherSummaryViewModel(
                        r.Id.Value,
                        r.Name.PublicDisplayName,
                        r.AcademicBackground.DegreeLevel.Value,
                        p.Name))
            .ToListAsync();
}
