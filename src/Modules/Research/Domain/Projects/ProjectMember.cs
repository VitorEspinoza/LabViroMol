using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

using Researchers;

public class ProjectMember : BaseEntity<ProjectMemberId>, ICreationAuditable, IModificationAuditable
{
    public ResearcherId ResearcherId { get; private set; }
    public ProjectRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }
    public bool IsActive => LeftAt is null;

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }

    private ProjectMember() { }

    internal ProjectMember(ResearcherId researcherId, ProjectRole role)
        : base(IdFactory.New<ProjectMemberId>())
    {
        ResearcherId = researcherId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    internal void UpdateRole(ProjectRole newRole, UserId updatedBy)
    {
        Role = newRole;
        MarkAsUpdated(updatedBy);
    }

    internal void RemoveFromProject()
    {
        if (LeftAt.HasValue) return;
        LeftAt = DateTimeOffset.UtcNow;
    }

    private void MarkAsUpdated(UserId updatedBy)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
