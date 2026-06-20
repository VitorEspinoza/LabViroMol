using System.Text.Json;
using System.Text.Json.Serialization;
using LabViroMol.Modules.Shared.Infrastructure.Converters;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        options.Converters.Add(new SmartEnumJsonConverterFactory());
        options.Converters.Add(new StrongIdJsonConverterFactory());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
