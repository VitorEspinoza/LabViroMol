using System.Reflection;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public sealed class PersistentEventTypeRegistry : IPersistentEventTypeRegistry
{
    private readonly Lazy<IReadOnlyDictionary<string, Type>> _byName = new(BuildMap);

    public string GetName(Type eventType) => eventType.FullName!;

    public Type? Resolve(string name) =>
        _byName.Value.TryGetValue(name, out var type) ? type : null;

    private static IReadOnlyDictionary<string, Type> BuildMap()
    {
        var map = new Dictionary<string, Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).ToArray()!;
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type is { IsClass: true, IsAbstract: false } && typeof(IPersistentEvent).IsAssignableFrom(type))
                    map[type.FullName!] = type;
            }
        }

        return map;
    }
}
