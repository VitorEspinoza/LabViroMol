namespace LabViroMol.Modules.Research.Infrastructure.Publications;

using LabViroMol.Modules.Research.Application.Publications.ViewModels;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class PublicationQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<PublicationSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<PublicationSummaryViewModel> query = context.Publications.AsNoTracking()
            .Select(p => new PublicationSummaryViewModel(
                p.Id.Value,
                p.Title,
                p.Doi,
                p.PublishedOn,
                p.PublicationDate,
                EF.Property<DateTimeOffset>(p, "CreatedAt"),
                p.Researchers
                    .OrderBy(pr => pr.Order)
                    .Select(pr => new PublicationAuthorViewModel(
                        context.Researchers
                            .Where(r => r.Id == pr.ResearcherId)
                            .Select(r => r.Name.CitationName != null && r.Name.CitationName.Length > 0
                                ? r.Name.CitationName
                                : r.Name.LastName.ToUpper() + ", " + r.Name.FirstName.Substring(0, 1) + ".")
                            .Single(),
                        pr.Order))
                    .ToList()));

        query = query.WhereSearch(request.Search, x => x.Title, x => x.Doi, x => x.PublishedOn);

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

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<PublicationAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<PublicationAdminSummaryViewModel> query = context.Publications.AsNoTracking()
            .Select(p => new PublicationAdminSummaryViewModel(
                p.Id.Value,
                p.Title,
                p.Doi,
                p.PublicationDate,
                p.Researchers
                    .OrderBy(pr => pr.Order)
                    .Select(pr => context.Researchers
                        .Where(r => r.Id == pr.ResearcherId)
                        .Select(r => r.Name.CitationName != null && r.Name.CitationName.Length > 0
                            ? r.Name.CitationName
                            : r.Name.LastName.ToUpper() + ", " + r.Name.FirstName.Substring(0, 1) + ".")
                        .Single())
                    .FirstOrDefault() ?? string.Empty));

        query = query.WhereSearch(request.Search, x => x.Title, x => x.Doi, x => x.CitationName);

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

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PublicationViewModel?> GetById(Guid id)
        => await context.Publications.AsNoTracking()
            .Include(p => p.Researchers)
            .Where(p => p.Id == PublicationId.From(id))
            .Select(p => new PublicationViewModel(
                p.Id.Value,
                p.Title,
                p.Description,
                p.Doi,
                p.PublicationDate,
                p.PublishedOn,
                p.PublishUrl,
                p.Researchers
                    .OrderBy(pr => pr.Order)
                    .Select(pr => new PublicationAuthorViewModel(
                        context.Researchers
                            .Where(r => r.Id == pr.ResearcherId)
                            .Select(r => r.Name.FullName)
                            .Single(),
                        pr.Order))
                    .ToList(),
                EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .FirstOrDefaultAsync();
}
