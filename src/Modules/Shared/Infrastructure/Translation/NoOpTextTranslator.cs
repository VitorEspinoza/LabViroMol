namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public sealed class NoOpTextTranslator : ITextTranslator
{
    public Task<string> TranslateAsync(string sourceLanguage, string targetLanguage, string text)
        => Task.FromResult(text);
}
