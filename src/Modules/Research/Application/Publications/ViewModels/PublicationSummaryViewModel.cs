namespace LabViroMol.Modules.Research.Application.Publications.ViewModels;

public record PublicationSummaryViewModel(
    Guid Id,
    string Title,
    string Doi,
    string PublishedOn,
    DateOnly PublicationDate,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<PublicationAuthorViewModel> Authors);
