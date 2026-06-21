using LabViroMol.Modules.Shared.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Observability;

public class PiiRedactionLogProcessorTests
{
    [Fact]
    public void OnEnd_RedactsSensitiveAttributes_AndKeepsBenignAttributesIntact()
    {
        var captured = new List<List<KeyValuePair<string, object?>>>();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(logging =>
            {
                logging.AddProcessor(new PiiRedactionLogProcessor());
                logging.AddProcessor(new CapturingProcessor(captured));
            });
        });

        var logger = loggerFactory.CreateLogger("PiiRedactionLogProcessorTests");

        logger.LogInformation(
            "Approving schedule Authorization={Authorization} password={password} RequestName={RequestName}",
            "Bearer eyJhbGciOiJIUzI1NiJ9.test",
            "123456",
            "ApproveScheduleCommand");

        var attributes = Assert.Single(captured).ToDictionary(a => a.Key, a => a.Value);

        Assert.Equal("[REDACTED]", attributes["Authorization"]);
        Assert.Equal("[REDACTED]", attributes["password"]);
        Assert.Equal("ApproveScheduleCommand", attributes["RequestName"]);
    }

    private sealed class CapturingProcessor(List<List<KeyValuePair<string, object?>>> sink) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data)
        {
            sink.Add(data.Attributes is null
                ? new List<KeyValuePair<string, object?>>()
                : new List<KeyValuePair<string, object?>>(data.Attributes));
        }
    }
}
