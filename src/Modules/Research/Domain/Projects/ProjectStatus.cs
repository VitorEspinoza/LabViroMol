using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

public record ProjectStatus : SmartEnum<ProjectStatus>
{
    public static readonly ProjectStatus Planned = new("Planned");
    public static readonly ProjectStatus InProgress = new("InProgress");
    public static readonly ProjectStatus Completed = new("Completed");
    public static readonly ProjectStatus Canceled = new("Canceled");
    
    private ProjectStatus(string value) : base(value) { }
}
