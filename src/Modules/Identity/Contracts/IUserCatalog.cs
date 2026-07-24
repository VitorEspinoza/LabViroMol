namespace LabViroMol.Modules.Identity.Contracts;

public interface IUserCatalog
{
    Task<Dictionary<Guid, string>> GetUserDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct);
}
