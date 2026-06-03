using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Infrastructure.Emails;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public sealed class SmtpEmailSender : ISendEmail
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(
        IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmail(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
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
    }
}