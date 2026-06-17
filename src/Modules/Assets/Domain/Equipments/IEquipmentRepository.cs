namespace LabViroMol.Modules.Assets.Domain.Equipments;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(Equipment equipment, CancellationToken ct);
    Task<Equipment?> GetByIdAsync(Guid id, CancellationToken ct);
    void Remove(Equipment equipment);
    Task<List<Equipment>> GetMissingEnglishTranslationAsync(int limit,
        CancellationToken ct);
}