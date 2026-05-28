using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Infrastructure.Storage.Configuration;

namespace LabViroMol.Modules.Assets.Infrastructure.Storage;

using Microsoft.Extensions.Options;

public class LocalImageStorageService : IImageStorageService
{
    private readonly StorageSettings _settings;

    public LocalImageStorageService(
        IOptions<StorageSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<string> SaveEquipmentImageAsync(
        Guid equipmentId,
        Stream stream,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(
            _settings.ImageFolderPath,
            "equipments");

        Directory.CreateDirectory(folder);

        var fileName = $"{equipmentId}{extension}";

        var fullPath = Path.Combine(folder, fileName);

        await using var fileStream = File.Create(fullPath);

        await stream.CopyToAsync(fileStream, cancellationToken);

        return $"/images/equipments/{fileName}";
    }
}