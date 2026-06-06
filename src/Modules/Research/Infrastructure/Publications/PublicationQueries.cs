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
        var all = await context.Publications.AsNoTracking()
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
                    .ToList()))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Title).ToList()
                : all.OrderBy(p => p.Title).ToList(),
            "doi" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Doi).ToList()
                : all.OrderBy(p => p.Doi).ToList(),
            _ => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.PublicationDate).ToList()
                : all.OrderByDescending(p => p.PublicationDate).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<PagedResponse<PublicationAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var all = await context.Publications.AsNoTracking()
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
                    .FirstOrDefault() ?? string.Empty))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Title).ToList()
                : all.OrderBy(p => p.Title).ToList(),
            "doi" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Doi).ToList()
                : all.OrderBy(p => p.Doi).ToList(),
            _ => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.PublicationDate).ToList()
                : all.OrderByDescending(p => p.PublicationDate).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<IReadOnlyCollection<PublicationSummaryViewModel>> GetAll()
        => await context.Publications.AsNoTracking()
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
                            .Select(r => r.Name.FullName)
                            .Single(),
                        pr.Order))
                    .ToList()))
            .ToListAsync();

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
