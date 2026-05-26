using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

using Researchers;

public class ProjectMember : BaseEntity<ProjectMemberId>, IFullAuditable
{
    public ProjectRole Role { get; private set; }
    private ProjectMember() { }
    internal ProjectMember(ResearcherId researcherId, ProjectRole role)
        : base(ProjectMemberId.From(researcherId))
    {
        Role = role;
    }

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? RemovedAt { get; protected set; }
    public UserId? RemovedBy { get; protected set; }

    internal void UpdateRole(ProjectRole newRole, UserId updatedBy)
    {
        Role = newRole;
        MarkAsUpdated(updatedBy);
    }

    internal void MarkAsRemoved(UserId removedBy)
    {
        if (IsDeleted) return;

        RemovedAt = DateTimeOffset.UtcNow;
        RemovedBy = removedBy;
        IsDeleted = true;
    }

    internal void UndoRemove(UserId restoredBy)
    {
        RemovedAt = null;
        RemovedBy = null;
        IsDeleted = false;
        MarkAsUpdated(restoredBy);
    }

    private void MarkAsUpdated(UserId updatedBy)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
