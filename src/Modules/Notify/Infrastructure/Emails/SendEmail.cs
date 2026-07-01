using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Infrastructure.Emails;

using MailKit.Net.Smtp;
using MailKit.Security;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using Microsoft.Extensions.Options;
using MimeKit;

public sealed class SmtpEmailSender : ISendEmail
{
    private readonly EmailOptions _options;
    private readonly EmailMetrics _metrics;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        EmailMetrics metrics)
    {
        _options = options.Value;
        _metrics = metrics;
    }

    public async Task SendEmail(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
        var timer = _metrics.StartTimer();

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _options.SenderName,
                _options.SenderEmail));

        message.To.Add(
            MailboxAddress.Parse(to));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var smtp = new SmtpClient();

        try
        {
            await smtp.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                ct);

            await smtp.AuthenticateAsync(
                _options.Username,
                _options.Password,
                ct);

            await smtp.SendAsync(
                message,
                ct);

            await smtp.DisconnectAsync(
                true,
                ct);

            _metrics.RecordSuccess(timer);
        }
        catch
        {
            _metrics.RecordFailure(timer);
            throw;
        }
    }
}
