using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Notify.Infrastructure.Emails;

public sealed class BrevoEmailSender : ISendEmail
{
    private const string SendEmailPath = "smtp/email";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;
    private readonly EmailMetrics _metrics;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<EmailOptions> options,
        EmailMetrics metrics,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task SendEmail(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
        var timer = _metrics.StartTimer();

        var request = new BrevoEmailRequest
        {
            Sender = new BrevoContact
            {
                Email = _options.SenderEmail,
                Name = _options.SenderName
            },
            To = [new BrevoContact { Email = to }],
            Subject = subject,
            HtmlContent = htmlBody
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                SendEmailPath,
                request,
                SerializerOptions,
                ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                _logger.LogWarning("Envio de e-mail via Brevo excedeu a cota de requisições (HTTP 429).");

            response.EnsureSuccessStatusCode();

            _metrics.RecordSuccess(timer);
        }
        catch
        {
            _metrics.RecordFailure(timer);
            throw;
        }
    }
}
