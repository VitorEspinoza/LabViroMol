using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Shared.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly StorageSettings _settings;

    public LocalFileStorage(
        IOptions<StorageSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string folder,
        CancellationToken ct = default)
    {
        var targetFolder = Path.Combine(
            _settings.RootFolder,
            folder);

        Directory.CreateDirectory(targetFolder);

        var fullPath = Path.Combine(
            targetFolder,
            fileName);

        await using var fileStream = File.Create(fullPath);

        await stream.CopyToAsync(fileStream, ct);

        return $"/images/{folder}/{fileName}";
    }
}