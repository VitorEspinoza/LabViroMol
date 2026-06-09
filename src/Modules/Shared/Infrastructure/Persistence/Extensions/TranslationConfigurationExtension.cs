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
        
        var property = builder.Property(x => x.Translations);

        property.HasConversion(
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
            value => string.IsNullOrWhiteSpace(value)
                ? new Dictionary<string, TTranslation>()
                : JsonSerializer.Deserialize<Dictionary<string, TTranslation>>(
                    value,
                    JsonSerializerOptions.Default
                )!
        );

        return builder;
    }
}