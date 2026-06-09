namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class ProjectQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<PublicProjectViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language)
    {
        var all = await context.Projects
            .AsNoTracking()
            .Select(p => new PublicProjectViewModel(
                p.GetTitle(language),
                p.GetDescription(language),
                p.Status.Value,
                context.Researchers
                    .Where(r => p.Members.Any(m => m.Role == ProjectRole.ResearchLead
                                                   && m.LeftAt == null
                                                   && m.ResearcherId == r.Id))
                    .Select(r => r.Name.FullName)
                    .Single(),
                context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single()))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Title).ToList()
                : all.OrderBy(p => p.Title).ToList(),
            "status" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Status).ToList()
                : all.OrderBy(p => p.Status).ToList(),
            _ => all.OrderBy(p => p.Title).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<PagedResponse<ProjectAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var all = await context.Projects
            .AsNoTracking()
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

        var sorted = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Title).ToList()
                : all.OrderBy(p => p.Title).ToList(),
            "status" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Status).ToList()
                : all.OrderBy(p => p.Status).ToList(),
            "createdat" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.CreatedAt).ToList()
                : all.OrderBy(p => p.CreatedAt).ToList(),
            _ => all.OrderByDescending(p => p.CreatedAt).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<IReadOnlyCollection<ProjectSummaryViewModel>> GetAll()
        => await context.Projects
            .AsNoTracking()
            .Select(p => new ProjectSummaryViewModel(
                p.Id.Value,
                p.Title,
                p.Description,
                p.Status.Value,
                context.Researchers
                    .Where(r => p.Members.Any(m => m.Role == ProjectRole.ResearchLead
                                                   && m.LeftAt == null
                                                   && m.ResearcherId == r.Id))
                    .Select(r => r.Name.FullName)
                    .Single(),
                context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single(),
                EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .ToListAsync();

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
