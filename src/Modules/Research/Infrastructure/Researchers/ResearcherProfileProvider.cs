namespace LabViroMol.Modules.Research.Infrastructure.Researchers;

using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal class ResearcherProfileProvider(IResearcherRepository researcherRepository, ResearchDbContext context) : IResearcherProfileProvider
{
    public async Task<ResearchRegistrationData?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var researcher = await researcherRepository.GetByIdAsync(ResearcherId.From(userId), ct);

        if (researcher is null)
            return null;

        return new ResearchRegistrationData(
            researcher.PositionId.Value,
            researcher.AcademicBackground.DegreeLevel.Value,
            researcher.AcademicBackground.FieldOfStudy,
            researcher.LattesUrl,
            researcher.Name.CitationName,
            researcher.Name.DisplayName);
    }

    public async Task<Dictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().Select(ResearcherId.From).ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await context.Researchers.AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id.Value, r => r.Name.FullName, ct);
    }
}
