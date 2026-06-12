namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public interface ITranslationJob
{
    Task ExecuteAsync(CancellationToken ct);
}