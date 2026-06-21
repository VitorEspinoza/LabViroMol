using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class PiiRedactionLogProcessor : BaseProcessor<LogRecord>
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitiveKeyFragments =
    {
        "password",
        "token",
        "authorization",
        "secret",
        "api-key",
        "apikey",
        "hash",
    };

    public override void OnEnd(LogRecord data)
    {
        if (data.Attributes is null || data.Attributes.Count == 0)
        {
            return;
        }

        List<KeyValuePair<string, object?>>? redacted = null;

        for (var i = 0; i < data.Attributes.Count; i++)
        {
            var attribute = data.Attributes[i];

            if (!IsSensitive(attribute.Key, attribute.Value))
            {
                continue;
            }

            redacted ??= new List<KeyValuePair<string, object?>>(data.Attributes);
            redacted[i] = new KeyValuePair<string, object?>(attribute.Key, RedactedValue);
        }

        if (redacted is not null)
        {
            data.Attributes = redacted;
        }
    }

    private static bool IsSensitive(string key, object? value)
    {
        foreach (var fragment in SensitiveKeyFragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (value is string text)
        {
            if (text.StartsWith("eyJ", StringComparison.Ordinal) ||
                text.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
