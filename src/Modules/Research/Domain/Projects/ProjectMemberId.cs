using LabViroMol.Modules.Research.Domain.Researchers;

namespace LabViroMol.Modules.Research.Domain.Projects;

using LabViroMol.Modules.Shared.Kernel.Primitives;

public record struct ProjectMemberId(Guid Value) : IStrongId<ProjectMemberId>
{
    public static ProjectMemberId New() => new(Guid.CreateVersion7());
    
    public static ProjectMemberId From(Guid value) => new(value);
    
    public static implicit operator Guid(ProjectMemberId id) => id.Value;
}
