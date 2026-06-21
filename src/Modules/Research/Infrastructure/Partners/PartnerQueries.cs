using LabViroMol.Modules.Research.Domain.Partners;

namespace LabViroMol.Modules.Research.Infrastructure.Partners;

using LabViroMol.Modules.Research.Application.Partners.Queries;
using LabViroMol.Modules.Research.Application.Partners.ViewModels;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

internal sealed class PartnerQueries(ResearchDbContext context) : IPartnerQueries
{
    public async Task<PagedResponse<PartnerSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Partner> query = context.Partners.AsNoTracking();

        query = query.WhereSearch(request.Search, x => x.Name);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new PartnerSummaryViewModel(p.Id.Value, p.Name, EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<PartnerAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Partner> query = context.Partners.AsNoTracking();

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
            .Select(p => new PartnerAdminSummaryViewModel(p.Id.Value, p.Name, p.Description))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PartnerViewModel?> GetById(Guid id)
        => await context.Partners.AsNoTracking()
            .Where(p => p.Id == PartnerId.From(id))
            .Select(p => new PartnerViewModel(p.Id.Value, p.Name, p.Description, EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .FirstOrDefaultAsync();
}
