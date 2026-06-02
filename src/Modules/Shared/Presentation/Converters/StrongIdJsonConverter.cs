using System.Text.Json;
using System.Text.Json.Serialization;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Shared.Presentation.Converters;

public class StrongIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStrongId<>));
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StrongIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private class StrongIdJsonConverter<TId> : JsonConverter<TId> 
        where TId : struct, IStrongId<TId>
    {
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String || !reader.TryGetGuid(out var guid))
            {
                return default;
            }

            return TId.From(guid);
        }

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}