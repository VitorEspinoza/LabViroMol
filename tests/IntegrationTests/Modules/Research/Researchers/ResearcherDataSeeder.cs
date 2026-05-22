using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Research.IntegrationTests.Positions;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Research.IntegrationTests.Researchers;

public static class ResearcherDataSeeder
{
    public static async Task<(Guid researcherId, Guid positionId)> SeedResearcherAsync(ResearchDbContext dbContext)
    {
        var positionId = await PositionDataSeeder.SeedPositionAsync(dbContext);
        var userId = IdFactory.New<UserId>();
        var researcherId = IdFactory.New<ResearcherId>();

        var researcher = Researcher.Create(
            researcherId,
            userId,
            new ResearcherName("Ana", "Silva", null, null),
            null,
            new AcademicBackground(DegreeLevel.Doctorate, "Virologia"),
            PositionId.From(positionId));

        await dbContext.Researchers.AddAsync(researcher);
        await dbContext.SaveChangesAsync();

        return (researcherId.Value, positionId);
    }
}
