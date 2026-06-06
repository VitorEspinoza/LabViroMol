namespace LabViroMol.Modules.Research.Application.Publications.ViewModels;

public record PublicationAdminSummaryViewModel(
    Guid Id,
    string Title,
    string Doi,
    DateOnly PublicationDate,
    string CitationName);
