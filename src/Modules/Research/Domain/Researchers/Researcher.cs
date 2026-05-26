namespace LabViroMol.Modules.Research.Domain.Researchers;

using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

public class Researcher : AggregateRoot<ResearcherId>, IFullAuditable
{
    private Researcher() { }

    private Researcher(ResearcherId id,
        ResearcherName name,
        string? lattesUrl,
        AcademicBackground academicBackground,
        PositionId positionId)
        : base(id)
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

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? RemovedAt { get; protected set; }
    public UserId? RemovedBy { get; protected set; }

    public static Researcher Create(
        ResearcherId researcherId,
        ResearcherName name,
        string? lattesUrl,
        AcademicBackground academicBackground,
        PositionId positionId)
    {
        return new Researcher(
            researcherId,
            name, lattesUrl, academicBackground, positionId);
    }

    public void Update(DegreeLevel degreeLevel, string fieldOfStudy, PositionId positionId)
    {
        AcademicBackground = new AcademicBackground(degreeLevel, fieldOfStudy);
        PositionId = positionId;
    }
}
