namespace LabViroMol.Modules.Research.Domain.Positions;

public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(PositionId id, CancellationToken ct);
    Task AddAsync(Position position, CancellationToken ct);
    void Delete(Position position);
    Task<List<Position>> GetMissingEnglishTranslationAsync(int limit,
        CancellationToken ct);
}
