namespace LabViroMol.Modules.Research.Infrastructure.Researchers;

using LabViroMol.Modules.Research.Application.Researchers.ViewModels;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class ResearcherQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<ResearcherSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language)
    {
        var all = await context.Researchers.AsNoTracking()
            .Join(context.Positions,
                r => r.PositionId,
                p => p.Id,
                (r, p) =>
                     new ResearcherSummaryViewModel(
                        r.Id.Value,
                        r.Name.PublicDisplayName,
                        r.AcademicBackground.DegreeLevel.Value,
                        p.GetName(language),
                        r.LattesUrl))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? all.OrderByDescending(r => r.DisplayName).ToList()
                : all.OrderBy(r => r.DisplayName).ToList(),
            "position" => request.SortDirection == "desc"
                ? all.OrderByDescending(r => r.Position).ToList()
                : all.OrderBy(r => r.Position).ToList(),
            _ => all.OrderBy(r => r.DisplayName).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<IReadOnlyCollection<ResearcherSummaryViewModel>> GetAll()
        => await context.Researchers.AsNoTracking()
            .Join(context.Positions,
                r => r.PositionId,
                p => p.Id,
                (r, p) =>
                     new ResearcherSummaryViewModel(
                        r.Id.Value,
                        r.Name.PublicDisplayName,
                        r.AcademicBackground.DegreeLevel.Value,
                        p.Name,
                        r.LattesUrl))
            .ToListAsync();
}
