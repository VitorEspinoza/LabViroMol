using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly IOptionsMonitor<SlowQueryOptions> _options;

    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, IOptionsMonitor<SlowQueryOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogIfSlow(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        var thresholdMs = _options.CurrentValue.ThresholdMs;
        var durationMs = eventData.Duration.TotalMilliseconds;

        if (thresholdMs <= 0 || durationMs < thresholdMs)
        {
            return;
        }

        _logger.LogWarning(
            "Slow query detected: {CommandType} took {DurationMs}ms (threshold {ThresholdMs}ms)",
            command.CommandType,
            durationMs,
            thresholdMs);
    }
}
