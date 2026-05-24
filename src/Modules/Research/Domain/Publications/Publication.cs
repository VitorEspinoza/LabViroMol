using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Publications;

public class Publication : AggregateRoot<PublicationId>
{
    private Publication() { }

    private Publication(PublicationId id, UserId createdBy, string title, string description,
        string doi, DateOnly publicationDate, string publishedOn, string publishUrl)
        : base(id, createdBy)
    {
        Title = Guard.AgainstMinLength(title, 3, "O título deve ter ao menos 3 caracteres.");
        Description = description;
        Doi = doi;
        PublicationDate = publicationDate;
        PublishedOn = Guard.AgainstEmpty(publishedOn, "O local de publicação é obrigatório.");
        PublishUrl = publishUrl;
    }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Doi { get; private set; }
    public DateOnly PublicationDate { get; private set; }
    public string PublishedOn { get; private set; }
    public string PublishUrl { get; private set; }

    private readonly List<PublicationResearcher> _researchers = new();
    public IReadOnlyCollection<PublicationResearcher> Researchers => _researchers.AsReadOnly();

    public static Result<Publication> Create(UserId createdBy, string title, string description,
        string doi, DateOnly publicationDate, string publishedOn, string publishUrl)
    {
        var publication = new Publication(IdFactory.New<PublicationId>(), createdBy,
            title, description, doi, publicationDate, publishedOn, publishUrl);
        return Result<Publication>.Success(publication);
    }

    public Result Update(string title, string description,
        string publishedOn, string publishUrl, UserId modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            return Result.BusinessRule("O titulo deve ter ao menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(publishedOn))
            return Result.BusinessRule("O local de publicacao e obrigatorio.");

        Title = title;
        Description = description;
        PublishedOn = publishedOn;
        PublishUrl = publishUrl;

        MarkAsUpdated(modifiedBy);
        return Result.Success();
    }

    public Result AssignDoi(string doi, UserId modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(doi))
            return Result.BusinessRule("O DOI e obrigatorio.");

        Doi = doi;
        MarkAsUpdated(modifiedBy);
        return Result.Success();
    }

    public Result AddResearcher(ResearcherId researcherId, UserId modifiedBy)
    {
        if (_researchers.Any(r => r.ResearcherId == researcherId))
            return Result.Conflict("O pesquisador ja esta vinculado a esta publicacao.");

        var nextOrder = _researchers.Count > 0 ? _researchers.Max(r => r.Order) + 1 : 1;
        _researchers.Add(new PublicationResearcher(researcherId, nextOrder));

        MarkAsUpdated(modifiedBy);
        return Result.Success();
    }

    public Result RemoveResearcher(ResearcherId researcherId, UserId modifiedBy)
    {
        var researcher = _researchers.FirstOrDefault(r => r.ResearcherId == researcherId);
        if (researcher is null)
            return Result.NotFound("Pesquisador nao encontrado nesta publicacao.");

        _researchers.Remove(researcher);

        for (var i = 0; i < _researchers.Count; i++)
            _researchers[i] = _researchers[i] with { Order = i + 1 };

        MarkAsUpdated(modifiedBy);
        return Result.Success();
    }

    public Result ReorderResearchers(List<ResearcherId> orderedIds, UserId modifiedBy)
    {
        if (orderedIds.Distinct().Count() != orderedIds.Count)
            return Result.BusinessRule("A lista nao pode conter pesquisadores duplicados.");

        if (orderedIds.Count != _researchers.Count)
            return Result.BusinessRule("A lista deve conter todos os pesquisadores da publicacao.");

        var currentIds = _researchers.Select(r => r.ResearcherId).ToHashSet();
        if (!orderedIds.All(currentIds.Contains))
            return Result.BusinessRule("A lista contem pesquisadores que nao pertencem a esta publicacao.");

        _researchers.Clear();
        for (var i = 0; i < orderedIds.Count; i++)
            _researchers.Add(new PublicationResearcher(orderedIds[i], i + 1));

        MarkAsUpdated(modifiedBy);
        return Result.Success();
    }
}
