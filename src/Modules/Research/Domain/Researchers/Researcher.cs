namespace LabViroMol.Modules.Research.Domain.Researchers;

using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

public class Researcher : AggregateRoot<ResearcherId>
{
    private Researcher() { }

    private Researcher(ResearcherId id,
        UserId createdBy,
        ResearcherName name,
        string? lattesUrl, 
        AcademicBackground academicBackground, 
        PositionId positionId)
        : base(id, createdBy)
    {
        Name = name;
        LattesUrl = lattesUrl;
        AcademicBackground = academicBackground;
        PositionId = positionId;
    }

    public ResearcherName Name { get; private set; }
    public string? LattesUrl { get; private set; }
    public AcademicBackground AcademicBackground { get; private set; }
    public PositionId PositionId { get; private set; }

    public static Researcher Create(
        ResearcherId researcherId,
        UserId createdBy,
        ResearcherName name,
        string? lattesUrl,
        AcademicBackground academicBackground,
        PositionId positionId)
    {
        return new Researcher(
            researcherId, createdBy,
            name, lattesUrl, academicBackground, positionId);
    }

    public void Update(DegreeLevel degreeLevel, string fieldOfStudy, PositionId positionId, UserId modifiedBy)
    {
        AcademicBackground = new AcademicBackground(degreeLevel, fieldOfStudy);
        PositionId = positionId;
        MarkAsUpdated(modifiedBy);
    }
}
