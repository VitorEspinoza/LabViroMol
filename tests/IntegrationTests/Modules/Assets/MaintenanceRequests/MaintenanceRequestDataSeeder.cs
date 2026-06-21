using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Assets.IntegrationTests.Equipments;

namespace LabViroMol.Modules.Assets.IntegrationTests.MaintenanceRequests;

public static class MaintenanceRequestDataSeeder
{
    public static async Task<(Guid equipmentId, Guid maintenanceRequestId)> SeedRequestedAsync(AssetsDbContext dbContext)
    {
        var equipmentId = await EquipmentDataSeeder.SeedEquipmentAsync(dbContext);

        var maintenanceRequest = MaintenanceRequest.Create(
            "Manutenção preventiva",
            "Equipamento com ruído anormal",
            equipmentId).Data!;

        await dbContext.MaintenanceRequests.AddAsync(maintenanceRequest);
        await dbContext.SaveChangesAsync();

        return (equipmentId, maintenanceRequest.Id.Value);
    }
}
