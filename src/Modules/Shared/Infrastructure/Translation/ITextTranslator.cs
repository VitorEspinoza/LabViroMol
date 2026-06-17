namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public interface ITextTranslator
{
    Task<string> TranslateAsync(string sourceLanguage, string targetLanguage, string text);
}