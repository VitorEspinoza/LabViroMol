using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;

namespace LabViroMol.Modules.Assets.IntegrationTests.Equipments;

public static class EquipmentDataSeeder
{
    public static async Task<Guid> SeedEquipmentAsync(
        AssetsDbContext dbContext,
        string? code = null,
        string? location = null)
    {
        var equipment = Equipment.Create(
            name: "Microscópio Eletrônico",
            brand: "Zeiss",
            model: "EVO 10",
            code: code ?? Guid.NewGuid().ToString("N")[..12],
            description: "Microscópio para análises de amostras",
            location: location).Data!;

        await dbContext.Equipments.AddAsync(equipment);
        await dbContext.SaveChangesAsync();

        return equipment.Id.Value;
    }
}
