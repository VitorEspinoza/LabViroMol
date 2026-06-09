using GTranslate;
using GTranslate.Translators;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public class TextTranslator : ITextTranslator
{
    private readonly ITranslator _translator;
    private  readonly ILogger _logger;

    public TextTranslator(ITranslator translator, ILogger<TextTranslator> logger)
    {
        _translator = translator;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(
        string sourceLanguage,
        string targetLanguage,
        string text)
    {
        _logger.LogInformation($"Translating: '{text}' from '{sourceLanguage}' to '{targetLanguage}'");
        var result = await _translator.TranslateAsync(
            text,
            Language.GetLanguage(targetLanguage),
            Language.GetLanguage(sourceLanguage));
        
        _logger.LogInformation($"Translated: '{text}' to '{result.Translation}'");
        return result.Translation;
    }
}