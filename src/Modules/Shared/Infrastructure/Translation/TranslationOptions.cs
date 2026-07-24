namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public sealed class TranslationOptions
{
    public int IntervalMinutes { get; set; } = 30;
    public string BaseUrl { get; set; } = "http://libretranslate:5000";
}