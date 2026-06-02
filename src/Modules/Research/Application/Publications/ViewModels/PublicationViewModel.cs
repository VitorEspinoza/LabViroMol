namespace LabViroMol.Modules.Research.Application.Publications.ViewModels;

public record PublicationViewModel(
    Guid Id,
    string Title,
    string Description,
    string Doi,
    DateOnly PublicationDate,
    string PublishedOn,
    string PublishUrl,
    IReadOnlyCollection<PublicationAuthorViewModel> Authors,
    DateTimeOffset CreatedAt);
