using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Materials;

public class MaterialValidatorService
{
    private readonly IMaterialRepository _materialRepository;

    public MaterialValidatorService(IMaterialRepository materialRepository)
    {
        _materialRepository = materialRepository;
    }

    public async Task<Result> ValidateMaterialsExistAsync(IEnumerable<MaterialId> ids, CancellationToken ct)
    {
        var requestedIds = ids.Distinct().ToList();
        var foundIds = await _materialRepository.GetExistingIdsAsync(requestedIds, ct);

        if (foundIds.Count == requestedIds.Count) 
            return Result.Success();
        
        var missingIds = requestedIds.Except(foundIds).Select(id => id.Value.ToString()).ToList();
        return Result.NotFound($"Os materiais com os seguintes ids não foram encontrados: {string.Join(", ", missingIds)}");

    }
}
