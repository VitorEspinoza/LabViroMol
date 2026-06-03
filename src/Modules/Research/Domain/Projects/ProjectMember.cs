using System;
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

    private ProjectMember() { }

    internal ProjectMember(ResearcherId researcherId, ProjectRole role)
        : base(IdFactory.New<ProjectMemberId>())
    {
        ResearcherId = researcherId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    internal void UpdateRole(ProjectRole newRole)
    {
        Role = newRole;
    }

    internal void RemoveFromProject()
    {
        if (LeftAt.HasValue) return;
        LeftAt = DateTimeOffset.UtcNow;
    }
}
