using System.Reflection;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;

public static class ConventionExtensions
{
    public static void AddLabViroMolConventions(this ModelConfigurationBuilder configurationBuilder, Assembly assembly)
    {
        var strongIdTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStrongId<>)));

        foreach (var type in strongIdTypes)
        {
            var converterType = typeof(StrongIdConverter<>).MakeGenericType(type);
            configurationBuilder.Properties(type).HaveConversion(converterType);
        }
    }
}