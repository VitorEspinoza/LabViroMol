namespace LabViroMol.Modules.Research.Domain.Publications;

public interface IPublicationRepository
{
    Task<Publication?> GetByIdAsync(PublicationId id, CancellationToken ct);
    Task AddAsync(Publication publication, CancellationToken ct);
    void Delete(Publication publication);
}
