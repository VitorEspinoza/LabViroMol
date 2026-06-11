using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;

public static class TranslationConfigurationExtension
{
    public static EntityTypeBuilder<TEntity> ConfigureTranslations<TEntity, TTranslation>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITranslatable<TTranslation>
        where TTranslation : class
    {
        // EF Core não detecta mudanças em Dictionary com HasConversion por padrão,
        // pois usa igualdade por referência. O ValueComparer resolve isso.
        var comparer = new ValueComparer<Dictionary<string, TTranslation>>(
            (a, b) => JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b),
            v => v == null ? 0 : JsonSerializer.Serialize(v).GetHashCode(),
            v => v == null
                ? new Dictionary<string, TTranslation>()
                : JsonSerializer.Deserialize<Dictionary<string, TTranslation>>(
                    JsonSerializer.Serialize(v),
                    JsonSerializerOptions.Default)!
        );

        builder
            .Property(x => x.Translations)
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
                value => string.IsNullOrWhiteSpace(value)
                    ? new Dictionary<string, TTranslation>()
                    : JsonSerializer.Deserialize<Dictionary<string, TTranslation>>(
                        value,
                        JsonSerializerOptions.Default)!
            )
            .Metadata
            .SetValueComparer(comparer);

        return builder;
    }
}