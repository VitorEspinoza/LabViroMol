using System;

namespace LabViroMol.Modules.Research.Domain.Researchers;

using LabViroMol.Modules.Research.Domain.Positions;
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
    public DateTimeOffset? DeactivatedAt { get; private set; }
    public bool IsActive => DeactivatedAt == null;

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

    public void UpdateName(ResearcherName name)
    {
        Name = name;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("O pesquisador já está desativado.");
        DeactivatedAt = DateTimeOffset.UtcNow;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("O pesquisador já está ativo.");
        DeactivatedAt = null;
    }
}
