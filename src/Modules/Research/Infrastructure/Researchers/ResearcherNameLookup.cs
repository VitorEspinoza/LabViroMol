namespace LabViroMol.Modules.Research.Infrastructure.Researchers;

using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal static class ResearcherNameLookup
{
    public static async Task<Dictionary<ResearcherId, ResearcherName>> GetNamesAsync(
        ResearchDbContext context, IEnumerable<ResearcherId> researcherIds)
    {
        var ids = researcherIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<ResearcherId, ResearcherName>();

        return await context.Researchers.AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name);
    }
}
