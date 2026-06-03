using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class ProjectRepository(ResearchDbContext context) : IProjectRepository
{
    public async Task<Project?> GetByIdAsync(ProjectId id, CancellationToken ct)
        => await context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Project project, CancellationToken ct)
        => await context.Projects.AddAsync(project, ct);
}
