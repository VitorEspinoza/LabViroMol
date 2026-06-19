using System.Text.Json;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor<TContext> : IOutboxProcessor where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IMediator _mediator;
    private readonly IPersistentEventTypeRegistry _registry;
    private readonly ILogger<OutboxProcessor<TContext>> _logger;
    private readonly OutboxOptions _options;

    public OutboxProcessor(
        TContext context,
        IMediator mediator,
        IPersistentEventTypeRegistry registry,
        ILogger<OutboxProcessor<TContext>> logger,
        IOptions<OutboxOptions> options)
    {
        _context = context;
        _mediator = mediator;
        _registry = registry;
        _logger = logger;
        _options = options.Value;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var messages = await _context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                var eventType = _registry.Resolve(message.Type);
                if (eventType is null)
                {
                    message.MarkFailed($"Tipo de evento desconhecido: '{message.Type}'.");
                    _logger.LogError("OutboxMessage {Id} tem tipo desconhecido {Type}", message.Id, message.Type);
                    continue;
                }

                var @event = (IPersistentEvent)JsonSerializer.Deserialize(message.Content, eventType, OutboxJson.Options)!;

                await _mediator.Publish(@event, cancellationToken);

                message.MarkProcessed(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogError(ex, "Falha ao processar OutboxMessage {Id} ({Type})", message.Id, message.Type);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
