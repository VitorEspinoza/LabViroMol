using System.Diagnostics;
using System.Security.Claims;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Shared.Infrastructure.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;
    private readonly LabViroMolMetrics _metrics;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TMessage, TResponse>> logger,
        LabViroMolMetrics metrics,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _metrics = metrics;
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TMessage).Name;
        var userId = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        using var _ = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["UserId"] = userId,
        });

        var stopwatch = Stopwatch.StartNew();

        var response = await next(message, cancellationToken);

        stopwatch.Stop();
        var durationMs = stopwatch.Elapsed.TotalMilliseconds;

        _metrics.RecordDuration(requestName, durationMs);

        if (response is Result { IsFailure: true } failureResult)
        {
            var errorType = failureResult.ErrorType?.ToString() ?? "Unexpected";

            _logger.LogWarning(
                "Falha de regra de negócio em {RequestName}: {ErrorType}. Erros: {Errors}. Duração: {DurationMs}ms",
                requestName,
                errorType,
                failureResult.Errors,
                durationMs);

            _metrics.RecordFailure(requestName, errorType);

            return response;
        }

        if (response is Result)
        {
            _logger.LogInformation(
                "{RequestName} executado com sucesso em {DurationMs}ms",
                requestName,
                durationMs);

            _metrics.RecordSuccess(requestName);

            return response;
        }

        return response;
    }
}
