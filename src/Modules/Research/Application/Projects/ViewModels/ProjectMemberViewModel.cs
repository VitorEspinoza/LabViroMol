using LabViroMol.Modules.Research.Domain.Projects;

namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record ProjectMemberViewModel(Guid ResearcherId, string ResearcherName, ProjectRole Role);
