using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

public record ProjectRole : SmartEnum<ProjectRole>
{
    public static readonly ProjectRole ResearchLead = new("ResearchLead");
    public static readonly ProjectRole Manager = new("Manager");
    public static readonly ProjectRole Collaborator = new("Collaborator");
    
    private ProjectRole(string value) : base(value) { }
}
