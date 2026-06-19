using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Outbox;

public class BaseUnitOfWorkOutboxTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly PersistentEventTypeRegistry _registry = new();

    public BaseUnitOfWorkOutboxTests()
    {
        _currentUser.Id.Returns(UserId.From(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task CompleteAsync_persists_persistent_event_to_outbox_instead_of_publishing()
    {
        await using var context = TestContextFactory.New();
        var unitOfWork = new TestUnitOfWork(context, _mediator, _currentUser, _registry);

        var @event = new TestPersistentEvent(UserId.From(Guid.CreateVersion7()), "hello");
        unitOfWork.AddPersistentEvent(@event);

        await unitOfWork.CompleteAsync();

        var message = Assert.Single(context.Set<OutboxMessage>().ToList());
        Assert.Equal(typeof(TestPersistentEvent).FullName, message.Type);
        Assert.Null(message.ProcessedOn);
        Assert.Contains("hello", message.Content);

        await _mediator.DidNotReceive().Publish(Arg.Any<IPersistentEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_without_persistent_events_writes_no_outbox_rows()
    {
        await using var context = TestContextFactory.New();
        var unitOfWork = new TestUnitOfWork(context, _mediator, _currentUser, _registry);

        await unitOfWork.CompleteAsync();

        Assert.Empty(context.Set<OutboxMessage>().ToList());
    }
}
