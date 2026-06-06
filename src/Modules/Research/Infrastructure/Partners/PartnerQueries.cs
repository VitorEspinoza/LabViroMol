using LabViroMol.Modules.Research.Domain.Partners;

namespace LabViroMol.Modules.Research.Infrastructure.Partners;

using LabViroMol.Modules.Research.Application.Partners.ViewModels;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class PartnerQueries(ResearchDbContext context)
{
    public async Task<PagedResponse<PartnerSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request)
    {
        var all = await context.Partners.AsNoTracking()
            .Select(p => new PartnerSummaryViewModel(p.Id.Value, p.Name, EF.Property<DateTimeOffset>(p, "CreatedAt")))
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

    public async Task<PagedResponse<PartnerAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var all = await context.Partners.AsNoTracking()
            .Select(p => new PartnerAdminSummaryViewModel(p.Id.Value, p.Name, p.Description))
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

    public async Task<IReadOnlyCollection<PartnerSummaryViewModel>> GetAll()
        => await context.Partners.AsNoTracking()
            .Select(p => new PartnerSummaryViewModel(p.Id.Value, p.Name, EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .ToListAsync();

    public async Task<PartnerViewModel?> GetById(Guid id)
        => await context.Partners.AsNoTracking()
            .Where(p => p.Id == PartnerId.From(id))
            .Select(p => new PartnerViewModel(p.Id.Value, p.Name, p.Description, EF.Property<DateTimeOffset>(p, "CreatedAt")))
            .FirstOrDefaultAsync();
}
