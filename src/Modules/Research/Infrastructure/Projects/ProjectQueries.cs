namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Application.Projects.Queries;
using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Researchers;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

public class ProjectQueries(ResearchDbContext context, ICurrentUser currentUser) : IProjectQueries
{
    public async Task<PagedResponse<PublicProjectViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Project> query = context.Projects.AsNoTracking().Include(p => p.Members);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search;

            var matchingStatuses = Enum.GetValues<ProjectStatus>()
                .Where(s => s.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var ownFieldMatchIds = await context.Projects
                .Where(p => p.Title.Contains(search) || p.Description.Contains(search) ||
                            matchingStatuses.Contains(p.Status))
                .Select(p => p.Id)
                .ToListAsync();

            var matchingPartnerIds = await context.Partners
                .Where(pt => pt.Name.Contains(search))
                .Select(pt => pt.Id)
                .ToListAsync();

            var projectIdsByPartner = await context.Projects
                .Where(p => matchingPartnerIds.Contains(p.PartnerId))
                .Select(p => p.Id)
                .ToListAsync();

            var matchingResearcherIds = await context.Researchers
                .Where(r => r.Name.FirstName.Contains(search) || r.Name.LastName.Contains(search) ||
                            (r.Name.CitationName != null && r.Name.CitationName.Contains(search)))
                .Select(r => r.Id)
                .ToListAsync();

            var projectIdsByLead = await context.Projects
                .Where(p => p.Members.Any(m => m.Role == ProjectRole.ResearchLead && m.LeftAt == null
                                                && matchingResearcherIds.Contains(m.ResearcherId)))
                .Select(p => p.Id)
                .ToListAsync();

            var matchingIds = ownFieldMatchIds
                .Union(projectIdsByPartner)
                .Union(projectIdsByLead)
                .ToList();

            query = query.Where(p => matchingIds.Contains(p.Id));
        }

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

        var projects = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var leadIds = GetResearchLeadIds(projects);
        var partnerIds = projects.Select(p => p.PartnerId);

        var researcherNames = await ResearcherNameLookup.GetNamesAsync(context, leadIds);
        var partnerNames = await GetPartnerNamesAsync(partnerIds);

        var items = projects.Select(p =>
        {
            var leadId = GetResearchLeadId(p);
            var leadName = leadId.HasValue && researcherNames.TryGetValue(leadId.Value, out var lead)
                ? lead.FullName
                : string.Empty;
            var partnerName = partnerNames.GetValueOrDefault(p.PartnerId, string.Empty);

            return new PublicProjectViewModel(p.Id.Value, p.GetTitle(language), p.GetDescription(language), p.Status.ToString(), leadName, partnerName);
        }).ToList();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResponse<ProjectAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Project> query = context.Projects.AsNoTracking().Include(p => p.Members);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search;

            var matchingStatuses = Enum.GetValues<ProjectStatus>()
                .Where(s => s.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var ownFieldMatchIds = await context.Projects
                .Where(p => p.Title.Contains(search) || matchingStatuses.Contains(p.Status))
                .Select(p => p.Id)
                .ToListAsync();

            var matchingPartnerIds = await context.Partners
                .Where(pt => pt.Name.Contains(search))
                .Select(pt => pt.Id)
                .ToListAsync();

            var projectIdsByPartner = await context.Projects
                .Where(p => matchingPartnerIds.Contains(p.PartnerId))
                .Select(p => p.Id)
                .ToListAsync();

            var matchingIds = ownFieldMatchIds.Union(projectIdsByPartner).ToList();

            query = query.Where(p => matchingIds.Contains(p.Id));
        }

        var totalCount = await query.CountAsync();

        var sortBy = request.SortBy?.ToLower();

        if (sortBy is "partnername" or "managername")
        {
            var allRows = await query
                .Select(p => new { Project = p, CreatedAt = EF.Property<DateTimeOffset>(p, "CreatedAt") })
                .ToListAsync();

            var allLeadIds = GetResearchLeadIds(allRows.Select(r => r.Project));
            var allPartnerIds = allRows.Select(r => r.Project.PartnerId);

            var allResearcherNames = await ResearcherNameLookup.GetNamesAsync(context, allLeadIds);
            var allPartnerNames = await GetPartnerNamesAsync(allPartnerIds);

            var allItems = allRows.Select(r =>
            {
                var p = r.Project;
                var leadId = GetResearchLeadId(p);
                var leadName = leadId.HasValue && allResearcherNames.TryGetValue(leadId.Value, out var lead)
                    ? lead.FullName
                    : string.Empty;
                var partnerName = allPartnerNames.GetValueOrDefault(p.PartnerId, string.Empty);

                return new ProjectAdminSummaryViewModel(p.Id.Value, p.Title, partnerName, leadName, p.Status.ToString(), r.CreatedAt);
            });

            allItems = sortBy == "partnername"
                ? (request.SortDirection == "desc"
                    ? allItems.OrderByDescending(x => x.PartnerName)
                    : allItems.OrderBy(x => x.PartnerName))
                : (request.SortDirection == "desc"
                    ? allItems.OrderByDescending(x => x.ManagerName)
                    : allItems.OrderBy(x => x.ManagerName));

            var page = allItems.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return PagedResult.Create(page, pageNumber, pageSize, totalCount);
        }

        query = sortBy switch
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

        var rows = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new { Project = p, CreatedAt = EF.Property<DateTimeOffset>(p, "CreatedAt") })
            .ToListAsync();

        var leadIds = GetResearchLeadIds(rows.Select(r => r.Project));
        var partnerIds = rows.Select(r => r.Project.PartnerId);

        var researcherNames = await ResearcherNameLookup.GetNamesAsync(context, leadIds);
        var partnerNames = await GetPartnerNamesAsync(partnerIds);

        var items = rows.Select(r =>
        {
            var p = r.Project;
            var leadId = GetResearchLeadId(p);
            var leadName = leadId.HasValue && researcherNames.TryGetValue(leadId.Value, out var lead)
                ? lead.FullName
                : string.Empty;
            var partnerName = partnerNames.GetValueOrDefault(p.PartnerId, string.Empty);

            return new ProjectAdminSummaryViewModel(p.Id.Value, p.Title, partnerName, leadName, p.Status.ToString(), r.CreatedAt);
        }).ToList();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<ProjectViewModel?> GetById(Guid id)
    {
        var row = await context.Projects.AsNoTracking()
            .Include(p => p.Members)
            .Where(p => p.Id == ProjectId.From(id))
            .Select(p => new { Project = p, CreatedAt = EF.Property<DateTimeOffset>(p, "CreatedAt") })
            .FirstOrDefaultAsync();

        if (row is null)
            return null;

        var project = row.Project;
        var activeMembers = project.Members.Where(m => m.LeftAt == null).ToList();

        var researcherNames = await ResearcherNameLookup.GetNamesAsync(context, activeMembers.Select(m => m.ResearcherId));
        var partnerNames = await GetPartnerNamesAsync([project.PartnerId]);

        var currentUserGuid = currentUser.Id.Value;
        var currentMember = activeMembers.FirstOrDefault(m => m.ResearcherId.Value == currentUserGuid);
        var isLead = currentMember?.Role == ProjectRole.ResearchLead;
        var hasAdminPriv = isLead || currentMember?.Role == ProjectRole.Manager;

        return new ProjectViewModel(
            project.Id.Value,
            project.Title,
            project.Description,
            project.Status,
            project.PartnerId.Value,
            partnerNames.GetValueOrDefault(project.PartnerId, string.Empty),
            activeMembers.Select(m => new ProjectMemberViewModel(m.Id, researcherNames[m.ResearcherId].FullName, m.Role)).ToList(),
            row.CreatedAt,
            CanChangeStatus: isLead,
            CanTransferLeadership: isLead,
            CanEditMembers: hasAdminPriv,
            CanChangeMemberRole: hasAdminPriv,
            CanRemoveMembers: hasAdminPriv);
    }

    private static ResearcherId? GetResearchLeadId(Project project)
        => project.Members.FirstOrDefault(m => m.Role == ProjectRole.ResearchLead && m.LeftAt == null)?.ResearcherId;

    private static IEnumerable<ResearcherId> GetResearchLeadIds(IEnumerable<Project> projects)
        => projects.Select(GetResearchLeadId).Where(id => id.HasValue).Select(id => id!.Value);

    private async Task<Dictionary<PartnerId, string>> GetPartnerNamesAsync(IEnumerable<PartnerId> partnerIds)
    {
        var ids = partnerIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<PartnerId, string>();

        return await context.Partners.AsNoTracking()
            .Where(pt => ids.Contains(pt.Id))
            .ToDictionaryAsync(pt => pt.Id, pt => pt.Name);
    }
}
