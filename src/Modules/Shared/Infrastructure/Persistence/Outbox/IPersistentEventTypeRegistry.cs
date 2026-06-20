namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public interface IPersistentEventTypeRegistry
{
    string GetName(Type eventType);

    Type? Resolve(string name);
}
