namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ProjectQueries(ResearchDbContext context)
{
    public async Task<IReadOnlyCollection<ProjectSummaryViewModel>> GetAll()
    {
        var result = await context.Projects
            .AsNoTracking()
            .Include(p => p.Members)
            .Join(
                context.Partners,
                p => p.PartnerId,
                pt => pt.Id,
                (p, pt) => new { Project = p, Partner = pt })
            .ToListAsync();

        var researcherIds = result
            .SelectMany(x => x.Project.Members
                .Where(m => m.Role == ProjectRole.ResearchLead)
                .Select(m => (Guid)m.Id))
            .ToList();

        var researchers = await context.Researchers
            .AsNoTracking()
            .Where(r => researcherIds.Contains((Guid)r.Id))
            .Select(r => new { Id = (Guid) r.Id, Name = r.Name.FullName })
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        return result.Select(x =>
        {
            var leadId = x.Project.Members
                .First(m => m.Role == ProjectRole.ResearchLead)
                .Id.Value;

            return new ProjectSummaryViewModel(
                x.Project.Id.Value,
                x.Project.Title,
                x.Project.Description,
                x.Project.Status.Value,
                researchers.GetValueOrDefault(leadId, "Desconhecido"),
                x.Partner.Name,
                x.Project.CreatedAt);
        }).ToList();
    }
    public async Task<ProjectViewModel?> GetById(Guid id)
    {
        var project = await context.Projects
            .AsNoTracking()
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == ProjectId.From(id));

        if (project is null) return null;

        var partner = await context.Partners
            .AsNoTracking()
            .Where(pt => pt.Id == project.PartnerId)
            .Select(pt => new { pt.Id, pt.Name })
            .FirstOrDefaultAsync();

        var memberIds = project.Members
            .Select(m => (Guid) m.Id)
            .ToList();

        var researchers = await context.Researchers
            .AsNoTracking()
            .Where(r => memberIds.Contains((Guid) r.Id))
            .Select(r => new { Id = r.Id.Value, Name = r.Name.FullName })
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        var members = project.Members.Select(m => new ProjectMemberViewModel(
            m.Id,
            researchers.GetValueOrDefault(m.Id.Value, "Desconhecido"),
            m.Role)).ToList();

        return new ProjectViewModel(
            project.Id.Value,
            project.Title,
            project.Description,
            project.Status,
            project.PartnerId.Value,
            partner?.Name ?? "Desconhecido",
            members,
            project.CreatedAt);
    }
}
