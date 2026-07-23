using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using LabViroMol.Modules.Notify.Infrastructure.Emails;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LabViroMol.Modules.Notify.Infrastructure.UnitTests.Emails;

public class BrevoEmailSenderTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent("{}")
            };
        }
    }

    private static readonly EmailOptions Options = new()
    {
        ApiKey = "super-secret-api-key",
        SenderName = "LabViroMol",
        SenderEmail = "no-reply@labviromol.test"
    };

    private static (BrevoEmailSender Sender, FakeHttpMessageHandler Handler, ILogger<BrevoEmailSender> Logger) CreateSender(
        HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(statusCode);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", Options.ApiKey);

        var logger = Substitute.For<ILogger<BrevoEmailSender>>();
        var metrics = new EmailMetrics();
        var sender = new BrevoEmailSender(httpClient, Microsoft.Extensions.Options.Options.Create(Options), metrics, logger);

        return (sender, handler, logger);
    }

    private static async Task<(int LatencyMeasurements, long FailureCount)> RunWithMetricsAsync(Func<Task> action)
    {
        var latencyMeasurements = 0;
        var failureCount = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name != LabViroMolDiagnostics.Name)
                return;

            if (instrument.Name is "email.latency" or "email.failures")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
        {
            if (instrument.Name == "email.latency")
                latencyMeasurements++;
        });
        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "email.failures")
                failureCount += measurement;
        });
        listener.Start();

        try
        {
            await action();
        }
        catch
        {
        }

        return (latencyMeasurements, failureCount);
    }

    [Fact]
    public async Task SendEmail_Http200_DoesNotThrow()
    {
        var (sender, _, _) = CreateSender(HttpStatusCode.OK);

        var exception = await Record.ExceptionAsync(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendEmail_Http200_EmitsLatencyWithoutIncrementingFailures()
    {
        var (sender, _, _) = CreateSender(HttpStatusCode.OK);

        var (latencyMeasurements, failureCount) = await RunWithMetricsAsync(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));

        Assert.Equal(1, latencyMeasurements);
        Assert.Equal(0, failureCount);
    }

    [Fact]
    public async Task SendEmail_GenericHttpFailure_PropagatesException()
    {
        var (sender, _, _) = CreateSender(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));
    }

    [Fact]
    public async Task SendEmail_GenericHttpFailure_IncrementsFailureCounter()
    {
        var (sender, _, _) = CreateSender(HttpStatusCode.Unauthorized);

        var (_, failureCount) = await RunWithMetricsAsync(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));

        Assert.Equal(1, failureCount);
    }

    [Fact]
    public async Task SendEmail_Http429_LogsDistinctWarning()
    {
        var (sender, _, logger) = CreateSender(HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));

        logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendEmail_Http429_StillPropagatesExceptionForOutboxRetry()
    {
        var (sender, _, _) = CreateSender(HttpStatusCode.TooManyRequests);

        var exception = await Record.ExceptionAsync(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<HttpRequestException>(exception);
    }

    [Fact]
    public async Task SendEmail_Http500_DoesNotLogTooManyRequestsWarning()
    {
        var (sender, _, logger) = CreateSender(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None));

        logger.DidNotReceive().Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendEmail_SerializesPayloadInCamelCaseWithoutLeakingApiKeyInBody()
    {
        var (sender, handler, _) = CreateSender(HttpStatusCode.OK);

        await sender.SendEmail("dest@example.com", "Assunto do e-mail", "<p>corpo do e-mail</p>", CancellationToken.None);

        Assert.NotNull(handler.LastRequestBody);
        var body = handler.LastRequestBody!;

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("sender", out var senderElement));
        Assert.True(senderElement.TryGetProperty("email", out var senderEmail));
        Assert.Equal(Options.SenderEmail, senderEmail.GetString());

        Assert.True(root.TryGetProperty("to", out var toElement));
        Assert.Equal(JsonValueKind.Array, toElement.ValueKind);
        Assert.Equal("dest@example.com", toElement[0].GetProperty("email").GetString());

        Assert.True(root.TryGetProperty("subject", out var subjectElement));
        Assert.Equal("Assunto do e-mail", subjectElement.GetString());

        Assert.True(root.TryGetProperty("htmlContent", out var htmlContentElement));
        Assert.Equal("<p>corpo do e-mail</p>", htmlContentElement.GetString());

        Assert.DoesNotContain(Options.ApiKey, body);
        Assert.DoesNotContain("apiKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api-key", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendEmail_SetsApiKeyOnlyOnRequestHeaderNotInBody()
    {
        var (sender, handler, _) = CreateSender(HttpStatusCode.OK);

        await sender.SendEmail("dest@example.com", "Assunto", "<p>corpo</p>", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.TryGetValues("api-key", out var values));
        Assert.Contains(Options.ApiKey, values!);
    }
}
