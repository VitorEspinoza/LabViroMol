namespace LabViroMol.Modules.Research.Application.Researchers.ViewModels;

public record ResearcherSummaryViewModel(
    Guid Id,
    string DisplayName,
    string DegreeLevel,
    string Position);
