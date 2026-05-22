namespace LabViroMol.Modules.Research.Domain.Projects;

using LabViroMol.Modules.Shared.Abstractions.Primitives;

public record struct ProjectId(Guid Value) : IStrongId<ProjectId>
{
    public static ProjectId New() => new(Guid.CreateVersion7());
    public static ProjectId From(Guid value) => new(value);
    public static implicit operator Guid(ProjectId id) => id.Value;
}
