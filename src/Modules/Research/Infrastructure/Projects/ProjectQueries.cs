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
                    .Where(r => p.Members.Any(m => m.Role == ProjectRole.ResearchLead && m.Id == r.Id)) 
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
                context.Partners.Where(pt => pt.Id == p.PartnerId).Select(pt => pt.Name).Single(),
                p.Members.Select(m =>
                    new ProjectMemberViewModel(m.Id,
                        context.Researchers
                            .Where(r => r.Id == m.Id)
                            .Select(r => r.Name.FullName)
                            .Single(),
                        m.Role)).ToList(),
                p.CreatedAt))
            .FirstOrDefaultAsync();
}
