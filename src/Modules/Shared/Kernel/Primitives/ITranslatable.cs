namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public interface ITranslatable<TTranslation>
{
    Dictionary<string, TTranslation> Translations { get; }
}