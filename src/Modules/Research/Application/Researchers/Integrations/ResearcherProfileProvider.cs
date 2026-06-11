using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Research.Domain.Researchers;

namespace LabViroMol.Modules.Research.Application.Researchers.Integrations;

internal class ResearcherProfileProvider(IResearcherRepository researcherRepository) : IResearcherProfileProvider
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
}
