namespace LabViroMol.Modules.Research.Contracts;

public interface IProjectCatalog
{
    Task<Dictionary<Guid, string>> GetProjectTitlesAsync(IEnumerable<Guid> projectIds, CancellationToken ct);
}
