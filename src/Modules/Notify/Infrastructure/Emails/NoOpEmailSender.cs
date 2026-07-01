using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Infrastructure.Observability;

namespace LabViroMol.Modules.Notify.Infrastructure.Emails;

public sealed class NoOpEmailSender : ISendEmail
{
    private readonly EmailMetrics _metrics;

    public NoOpEmailSender(EmailMetrics metrics)
    {
        _metrics = metrics;
    }

    public Task SendEmail(string to, string subject, string htmlBody, CancellationToken ct)
    {
        var timer = _metrics.StartTimer();
        _metrics.RecordSuccess(timer);
        return Task.CompletedTask;
    }
}
