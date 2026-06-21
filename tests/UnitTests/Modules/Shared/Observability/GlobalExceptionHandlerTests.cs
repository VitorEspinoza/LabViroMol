using System.Text.Json;
using LabViroMol.Modules.Shared.Infrastructure.Exceptions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Observability;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger =
        Substitute.For<ILogger<GlobalExceptionHandler>>();

    private GlobalExceptionHandler CreateHandler() => new(_logger);

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<ProblemDetails>(httpContext.Response.Body))!;
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_Returns422AndLogsWarning()
    {
        var handler = CreateHandler();
        var httpContext = CreateHttpContext();
        var exception = new DomainException("regra violada");

        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);
        _logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_Returns500AndLogsError()
    {
        var handler = CreateHandler();
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("erro inesperado");

        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        _logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_DoesNotLogWarning()
    {
        var handler = CreateHandler();
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("erro inesperado");

        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _logger.DidNotReceive().Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_ProblemDetailsContainsNonEmptyTraceId()
    {
        var handler = CreateHandler();
        var httpContext = CreateHttpContext();
        httpContext.TraceIdentifier = "fallback-trace-id";
        var exception = new DomainException("regra violada");

        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await ReadProblemDetailsAsync(httpContext);
        var traceId = problemDetails.Extensions["traceId"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_ProblemDetailsContainsNonEmptyTraceId()
    {
        var handler = CreateHandler();
        var httpContext = CreateHttpContext();
        httpContext.TraceIdentifier = "fallback-trace-id";
        var exception = new InvalidOperationException("erro inesperado");

        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await ReadProblemDetailsAsync(httpContext);
        var traceId = problemDetails.Extensions["traceId"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
    }

    [Fact]
    public async Task TryHandleAsync_AnyException_ReturnsTrue()
    {
        var handler = CreateHandler();
        var httpContext = CreateHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new DomainException("x"), CancellationToken.None);

        Assert.True(handled);
    }
}

