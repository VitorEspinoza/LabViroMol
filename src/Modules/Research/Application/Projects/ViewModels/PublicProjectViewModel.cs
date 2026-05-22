namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record PublicProjectViewModel(
    string Title,
    string Description,
    string Status,
    string ResearchLead,
    string Partner);
