using System.Diagnostics.Metrics;
using LabViroMol.Modules.Shared.Infrastructure.Behaviors;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Observability;

public record TestCommand(string Name) : ICommand<Result>;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestCommand, Result>> _logger =
        Substitute.For<ILogger<LoggingBehavior<TestCommand, Result>>>();
    private readonly IHttpContextAccessor _httpContextAccessor =
        Substitute.For<IHttpContextAccessor>();
    private readonly LabViroMolMetrics _metrics = new();

    private LoggingBehavior<TestCommand, Result> CreateBehavior() =>
        new(_logger, _metrics, _httpContextAccessor);

    [Fact]
    public async Task Handle_FailureResult_LogsWarning()
    {
        var behavior = CreateBehavior();
        var command = new TestCommand("x");
        MessageHandlerDelegate<TestCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.BusinessRule("regra violada"));

        await behavior.Handle(command, next, CancellationToken.None);

        _logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_NotFoundResult_LogsWarning()
    {
        var behavior = CreateBehavior();
        var command = new TestCommand("x");
        MessageHandlerDelegate<TestCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.NotFound("não encontrado"));

        await behavior.Handle(command, next, CancellationToken.None);

        _logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_SuccessResult_DoesNotLogWarning()
    {
        var behavior = CreateBehavior();
        var command = new TestCommand("x");
        MessageHandlerDelegate<TestCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        await behavior.Handle(command, next, CancellationToken.None);

        _logger.DidNotReceive().Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_SuccessResult_RecordsSuccessOutcomeMetric()
    {
        var measurements = new List<(string Outcome, string ErrorType)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == LabViroMolDiagnostics.Name && instrument.Name == "cqrs.requests")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string? outcome = null;
            string? errorType = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "outcome") outcome = tag.Value?.ToString();
                if (tag.Key == "error_type") errorType = tag.Value?.ToString();
            }
            measurements.Add((outcome ?? string.Empty, errorType ?? string.Empty));
        });
        listener.Start();

        var behavior = CreateBehavior();
        var command = new TestCommand("x");
        MessageHandlerDelegate<TestCommand, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        await behavior.Handle(command, next, CancellationToken.None);

        Assert.Contains(measurements, m => m.Outcome == "success" && m.ErrorType == "none");
    }
}

