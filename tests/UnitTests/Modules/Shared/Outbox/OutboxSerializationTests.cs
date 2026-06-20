using System.Text.Json;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Outbox;

public class OutboxSerializationTests
{
    [Fact]
    public void OutboxJson_roundtrips_event_with_strong_typed_id()
    {
        var userId = UserId.From(Guid.CreateVersion7());
        var original = new TestPersistentEvent(userId, "round-trip");

        var json = JsonSerializer.Serialize(original, typeof(TestPersistentEvent), OutboxJson.Options);
        var restored = (TestPersistentEvent)JsonSerializer.Deserialize(json, typeof(TestPersistentEvent), OutboxJson.Options)!;

        Assert.Equal(userId, restored.UserId);
        Assert.Equal("round-trip", restored.Name);
    }

    [Fact]
    public void Registry_resolves_persistent_event_type_by_name()
    {
        var registry = new PersistentEventTypeRegistry();

        var name = registry.GetName(typeof(TestPersistentEvent));

        Assert.Equal(typeof(TestPersistentEvent), registry.Resolve(name));
    }

    [Fact]
    public void Registry_returns_null_for_unknown_name()
    {
        var registry = new PersistentEventTypeRegistry();

        Assert.Null(registry.Resolve("Some.Unknown.Type"));
    }
}
