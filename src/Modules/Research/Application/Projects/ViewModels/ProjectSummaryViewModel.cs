namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record ProjectSummaryViewModel(
    Guid Id,
    string Title,
    string Status,
    string ResearchLead,
    DateTimeOffset CreatedAt);
