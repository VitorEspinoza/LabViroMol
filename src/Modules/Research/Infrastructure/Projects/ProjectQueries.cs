namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class ProjectQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<PublicProjectViewModel>> GetAllInstitutionalAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<PublicProjectViewModel> query = context.Projects
            .AsNoTracking()
            .Select(p => new PublicProjectViewModel(
                p.Title,
                p.Description,
                p.Status.Value,
                context.Researchers
                    .Where(r => p.Members.Any(m => m.Role == ProjectRole.ResearchLead
                                                   && m.LeftAt == null
                                                   && m.ResearcherId == r.Id))
                    .Select(r => r.Name.FullName)
                    .Single(),
                context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single()));

        query = query.WhereSearch(request.Search,
            x => x.Title, x => x.Description, x => x.Status, x => x.ResearchLead, x => x.Partner);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Title)
                : query.OrderBy(p => p.Title),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Status)
                : query.OrderBy(p => p.Status),
            _ => query.OrderBy(p => p.Title)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<ProjectAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Project> query = context.Projects.AsNoTracking();

        query = query.WhereSearch(request.Search,
            p => p.Title,
            p => context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single(),
            p => p.Status.Value);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Title)
                : query.OrderBy(p => p.Title),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Status)
                : query.OrderBy(p => p.Status),
            "createdat" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => EF.Property<DateTimeOffset>(p, "CreatedAt"))
                : query.OrderBy(p => EF.Property<DateTimeOffset>(p, "CreatedAt")),
            _ => query.OrderByDescending(p => EF.Property<DateTimeOffset>(p, "CreatedAt"))
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new ProjectAdminSummaryViewModel(
                p.Id.Value,
                p.Title,
                context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single(),
                context.Researchers
                    .Where(r => p.Members.Any(m => m.Role == ProjectRole.ResearchLead
                                                   && m.LeftAt == null
                                                   && m.ResearcherId == r.Id))
                    .Select(r => r.Name.FullName)
                    .Single(),
                p.Status.Value,
                EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<ProjectViewModel?> GetById(Guid id)
        => await context.Projects.AsNoTracking()
            .Where(p => p.Id == ProjectId.From(id))
            .Select(p => new ProjectViewModel(
                p.Id.Value,
                p.Title,
                p.Description,
                p.Status,
                p.PartnerId.Value,
                context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single(),
                p.Members.Where(m => m.LeftAt == null).Select(m =>
                    new ProjectMemberViewModel(m.Id,
                        context.Researchers
                            .Where(r => r.Id == m.ResearcherId)
                            .Select(r => r.Name.FullName)
                            .Single(),
                        m.Role)).ToList(),
                EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .FirstOrDefaultAsync();
}
