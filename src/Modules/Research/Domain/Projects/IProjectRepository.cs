namespace LabViroMol.Modules.Research.Domain.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(ProjectId id, CancellationToken ct);
    Task AddAsync(Project project, CancellationToken ct);
    Task<List<Project>> GetMissingEnglishTranslationAsync(int limit,
        CancellationToken ct);
}
