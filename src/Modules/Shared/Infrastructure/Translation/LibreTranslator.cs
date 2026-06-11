using System.Net.Http.Json;

namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public class LibreTranslator(
    HttpClient httpClient)
    : ITextTranslator
{
    public async Task<string> TranslateAsync(
        string sourceLanguage,
        string targetLanguage,
        string text)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/translate",
            new
            {
                q = text,
                source = sourceLanguage,
                target = targetLanguage
            });

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<TranslationResponse>();

        return result?.TranslatedText ?? text;
    }

    private sealed record TranslationResponse(
        string TranslatedText);
}