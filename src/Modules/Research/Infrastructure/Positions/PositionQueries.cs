namespace LabViroMol.Modules.Research.Infrastructure.Positions;

using LabViroMol.Modules.Research.Application.Positions.ViewModels;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class PositionQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<PositionViewModel>> GetAllAsync(PagedRequest request)
    {
        var all = await context.Positions.AsNoTracking()
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? all.OrderByDescending(p => p.Name).ToList()
                : all.OrderBy(p => p.Name).ToList(),
            _ => all.OrderBy(p => p.Name).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<IReadOnlyCollection<PositionViewModel>> GetAll()
        => await context.Positions.AsNoTracking()
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .ToListAsync();

    public async Task<PositionViewModel?> GetById(Guid id)
        => await context.Positions.AsNoTracking()
            .Where(p => p.Id == PositionId.From(id))
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .FirstOrDefaultAsync();
}
