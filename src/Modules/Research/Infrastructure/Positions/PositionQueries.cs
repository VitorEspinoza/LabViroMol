namespace LabViroMol.Modules.Research.Infrastructure.Positions;

using LabViroMol.Modules.Research.Application.Positions.Queries;
using LabViroMol.Modules.Research.Application.Positions.ViewModels;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class PositionQueries(ResearchDbContext context) : IPositionQueries
{
    public async Task<PagedResponse<PositionViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Position> query = context.Positions.AsNoTracking();

        query = query.WhereSearch(request.Search, x => x.Name, x => x.Description);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PositionViewModel?> GetById(Guid id)
        => await context.Positions.AsNoTracking()
            .Where(p => p.Id == PositionId.From(id))
            .Select(p => new PositionViewModel(p.Id.Value, p.Name, p.Description))
            .FirstOrDefaultAsync();
}
