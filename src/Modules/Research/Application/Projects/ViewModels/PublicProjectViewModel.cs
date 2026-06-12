namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record PublicProjectViewModel(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string ResearchLead,
    string Partner);
