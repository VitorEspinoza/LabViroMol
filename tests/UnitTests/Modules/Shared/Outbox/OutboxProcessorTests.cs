using System.Text.Json;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Outbox;

public class OutboxProcessorTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly PersistentEventTypeRegistry _registry = new();
    private readonly ILogger<OutboxProcessor<TestDbContext>> _logger =
        Substitute.For<ILogger<OutboxProcessor<TestDbContext>>>();

    private OutboxProcessor<TestDbContext> CreateProcessor(TestDbContext context) =>
        new(context, _mediator, _registry, _logger, Options.Create(new OutboxOptions()));

    private async Task<OutboxMessage> SeedMessageAsync(TestDbContext context)
    {
        var @event = new TestPersistentEvent(UserId.From(Guid.CreateVersion7()), "hi");
        var message = new OutboxMessage(
            _registry.GetName(typeof(TestPersistentEvent)),
            JsonSerializer.Serialize(@event, typeof(TestPersistentEvent), OutboxJson.Options),
            @event.OccurredOn);

        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    [Fact]
    public async Task ProcessAsync_publishes_event_and_marks_processed()
    {
        await using var context = TestContextFactory.New();
        await SeedMessageAsync(context);

        await CreateProcessor(context).ProcessAsync(CancellationToken.None);

        var stored = context.Set<OutboxMessage>().Single();
        Assert.NotNull(stored.ProcessedOn);
        Assert.Null(stored.Error);
        await _mediator.Received(1).Publish(Arg.Any<IPersistentEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_on_handler_failure_leaves_message_unprocessed_with_error()
    {
        await using var context = TestContextFactory.New();
        await SeedMessageAsync(context);

        _mediator
            .When(m => m.Publish(Arg.Any<IPersistentEvent>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("boom"));

        await CreateProcessor(context).ProcessAsync(CancellationToken.None);

        var stored = context.Set<OutboxMessage>().Single();
        Assert.Null(stored.ProcessedOn);
        Assert.Equal(1, stored.RetryCount);
        Assert.Contains("boom", stored.Error);
    }

    [Fact]
    public async Task ProcessAsync_skips_already_processed_messages()
    {
        await using var context = TestContextFactory.New();
        var message = await SeedMessageAsync(context);
        message.MarkProcessed(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();

        await CreateProcessor(context).ProcessAsync(CancellationToken.None);

        await _mediator.DidNotReceive().Publish(Arg.Any<IPersistentEvent>(), Arg.Any<CancellationToken>());
    }
}
