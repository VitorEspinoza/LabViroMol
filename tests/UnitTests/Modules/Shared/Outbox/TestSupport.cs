using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Outbox;

public record TestPersistentEvent(UserId UserId, string Name) : IPersistentEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyPersistenceConfigs();
    }
}

public sealed class TestUnitOfWork : BaseUnitOfWork<TestDbContext>
{
    public TestUnitOfWork(
        TestDbContext context,
        IMediator mediator,
        ICurrentUser currentUser,
        IPersistentEventTypeRegistry registry)
        : base(context, mediator, currentUser, registry) { }
}

internal static class TestContextFactory
{
    public static TestDbContext New() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
