namespace LabViroMol.Modules.Research.Infrastructure.Publications;

using LabViroMol.Modules.Research.Application.Publications.ViewModels;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Researchers;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class PublicationQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<PublicationSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Publication> query = context.Publications.AsNoTracking();

        query = query.WhereSearch(request.Search, p => p.Title, p => p.Doi, p => p.PublishedOn);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Title)
                : query.OrderBy(p => p.Title),
            "doi" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Doi)
                : query.OrderBy(p => p.Doi),
            _ => query.OrderByDescending(p => p.PublicationDate)
        };

        var rows = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new { Publication = p, CreatedAt = EF.Property<DateTimeOffset>(p, "CreatedAt") })
            .ToListAsync();

        var researcherIds = rows.SelectMany(r => r.Publication.Researchers).Select(pr => pr.ResearcherId);
        var names = await ResearcherNameLookup.GetNamesAsync(context, researcherIds);

        var items = rows.Select(r => new PublicationSummaryViewModel(
            r.Publication.Id.Value,
            r.Publication.Title,
            r.Publication.Doi,
            r.Publication.PublishedOn,
            r.Publication.PublicationDate,
            r.CreatedAt,
            r.Publication.Researchers
                .OrderBy(pr => pr.Order)
                .Select(pr => new PublicationAuthorViewModel(pr.ResearcherId, names[pr.ResearcherId].PublicCitationName, pr.Order))
                .ToList())).ToList();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<PublicationAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Publication> query = context.Publications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search;
            query = query.Where(p =>
                p.Title.Contains(search) ||
                p.Doi.Contains(search) ||
                context.Researchers.Any(r =>
                    p.Researchers.Any(pr => pr.ResearcherId == r.Id) &&
                    (r.Name.FirstName.Contains(search) || r.Name.LastName.Contains(search) ||
                     (r.Name.CitationName != null && r.Name.CitationName.Contains(search)))));
        }

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Title)
                : query.OrderBy(p => p.Title),
            "doi" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Doi)
                : query.OrderBy(p => p.Doi),
            _ => query.OrderByDescending(p => p.PublicationDate)
        };

        var publications = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var researcherIds = publications.SelectMany(p => p.Researchers).Select(pr => pr.ResearcherId);
        var names = await ResearcherNameLookup.GetNamesAsync(context, researcherIds);

        var items = publications.Select(p =>
        {
            var authors = p.Researchers
                .OrderBy(pr => pr.Order)
                .Select(pr => new PublicationAuthorViewModel(pr.ResearcherId, names[pr.ResearcherId].PublicCitationName, pr.Order))
                .ToList();

            return new PublicationAdminSummaryViewModel(p.Id.Value, p.Title, p.Doi, p.PublicationDate, authors);
        }).ToList();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PublicationViewModel?> GetById(Guid id)
    {
        var row = await context.Publications.AsNoTracking()
            .Where(p => p.Id == PublicationId.From(id))
            .Select(p => new { Publication = p, CreatedAt = EF.Property<DateTimeOffset>(p, "CreatedAt") })
            .FirstOrDefaultAsync();

        if (row is null)
            return null;

        var publication = row.Publication;
        var researcherIds = publication.Researchers.Select(pr => pr.ResearcherId);
        var names = await ResearcherNameLookup.GetNamesAsync(context, researcherIds);

        return new PublicationViewModel(
            publication.Id.Value,
            publication.Title,
            publication.Description,
            publication.Doi,
            publication.PublicationDate,
            publication.PublishedOn,
            publication.PublishUrl,
            publication.Researchers
                .OrderBy(pr => pr.Order)
                .Select(pr => new PublicationAuthorViewModel(pr.ResearcherId, names[pr.ResearcherId].FullName, pr.Order))
                .ToList(),
            row.CreatedAt);
    }
}
