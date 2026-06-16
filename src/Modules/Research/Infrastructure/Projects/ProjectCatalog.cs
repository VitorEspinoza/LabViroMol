namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal class ProjectCatalog(ResearchDbContext context) : IProjectCatalog
{
    public async Task<Dictionary<Guid, string>> GetProjectTitlesAsync(IEnumerable<Guid> projectIds, CancellationToken ct)
    {
        var ids = projectIds.Distinct().Select(ProjectId.From).ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await context.Projects.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id.Value, p => p.Title, ct);
    }
}
