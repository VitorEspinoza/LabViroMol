namespace LabViroMol.Modules.Research.Infrastructure.Researchers;

using LabViroMol.Modules.Research.Application.Researchers.Queries;
using LabViroMol.Modules.Research.Application.Researchers.ViewModels;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class ResearcherQueries(ResearchDbContext context) : IResearcherQueries
{
    public async Task<PagedResponse<ResearcherSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var query = context.Researchers.AsNoTracking()
            .Join(context.Positions,
                r => r.PositionId,
                p => p.Id,
                (r, p) => new { Researcher = r, PositionName = p.GetName(language) });

        query = query.WhereSearch(request.Search,
            x => x.Researcher.Name.FirstName,
            x => x.Researcher.Name.LastName,
            x => x.Researcher.Name.DisplayName,
            x => x.PositionName);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "position" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.PositionName)
                : query.OrderBy(x => x.PositionName),
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Researcher.Name.LastName).ThenByDescending(x => x.Researcher.Name.FirstName)
                : query.OrderBy(x => x.Researcher.Name.LastName).ThenBy(x => x.Researcher.Name.FirstName),
            _ => query.OrderBy(x => x.Researcher.Name.LastName).ThenBy(x => x.Researcher.Name.FirstName)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new ResearcherSummaryViewModel(
                x.Researcher.Id.Value,
                x.Researcher.Name.PublicDisplayName,
                x.Researcher.AcademicBackground.DegreeLevel.Value,
                x.PositionName,
                x.Researcher.LattesUrl))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
