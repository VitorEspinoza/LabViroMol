namespace LabViroMol.Modules.Assets.Application.Shared;

public interface IImageStorageService
{
    Task<string> SaveEquipmentImageAsync(
        Guid equipmentId,
        Stream stream,
        string extension,
        CancellationToken cancellationToken = default);
}