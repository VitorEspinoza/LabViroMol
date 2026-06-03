using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Shared.Infrastructure.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string folder,
        CancellationToken ct = default);
}