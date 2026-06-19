namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public string Content { get; private set; } = null!;

    public DateTimeOffset OccurredOn { get; private set; }

    public DateTimeOffset? ProcessedOn { get; private set; }

    public string? Error { get; private set; }

    public int RetryCount { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string content, DateTimeOffset occurredOn)
    {
        Id = Guid.CreateVersion7();
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
    }

    public void MarkProcessed(DateTimeOffset when)
    {
        ProcessedOn = when;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}
