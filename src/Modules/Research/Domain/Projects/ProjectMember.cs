using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

using Researchers;

public class ProjectMember : AuditableEntity<ProjectMemberId>
{
    public ProjectRole Role { get; private set; }
    private ProjectMember() { }
    internal ProjectMember(ResearcherId researcherId, ProjectRole role, UserId createdBy)
        : base(ProjectMemberId.From(researcherId), createdBy)
    {
        Role = role;
    }

    internal void UpdateRole(ProjectRole newRole, UserId updatedBy)
    {
        Role = newRole;
        MarkAsUpdated(updatedBy);
    }

    internal void UndoRemove(UserId restoredBy)
    {
        RemovedAt = null;
        RemovedBy = null;
        IsDeleted = false;
        MarkAsUpdated(restoredBy);
    }
}