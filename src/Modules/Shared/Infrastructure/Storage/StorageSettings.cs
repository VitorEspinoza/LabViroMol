namespace LabViroMol.Modules.Shared.Infrastructure.Storage;

public class StorageSettings
{
    public string RootFolder { get; set; }
    public StorageFoldersSettings Folders { get; set; } = new();
}