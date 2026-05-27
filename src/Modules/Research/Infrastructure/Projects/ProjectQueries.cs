namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ProjectQueries(ResearchDbContext context)
{
    public async Task<IReadOnlyCollection<ProjectSummaryViewModel>> GetAll()
        => await context.Projects
            .AsNoTracking()
            .Select(p => new ProjectSummaryViewModel(
                p.Id.Value,
                p.Title,
                p.Status.ToString(),
                context.Researchers
                    .Where(r => p.Members.Any(m => m.Role == ProjectRole.ResearchLead
                        && m.LeftAt == null
                        && m.ResearcherId == r.Id))
                    .Select(r => r.Name.FullName)
                    .Single(),
                p.CreatedAt))
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
                p.Members.Where(m => m.LeftAt == null).Select(m =>
                    new ProjectMemberViewModel(m.ResearcherId,
                        context.Researchers
                            .Where(r => r.Id == m.ResearcherId)
                            .Select(r => r.Name.FullName)
                            .Single(),
                        m.Role)).ToList(),
                p.CreatedAt))
            .FirstOrDefaultAsync();
}
