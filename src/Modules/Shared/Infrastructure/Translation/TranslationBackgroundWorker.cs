using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public class TranslationBackgroundWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TranslationBackgroundWorker> _logger;
    private readonly TranslationOptions _options;

    public TranslationBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TranslationBackgroundWorker> logger,
        IOptions<TranslationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var jobs =
                scope.ServiceProvider
                    .GetServices<ITranslationJob>();

            foreach (var job in jobs)
            {
                try
                {
                    await job.ExecuteAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro ao executar job de tradução");
                }
            }

            await Task.Delay(
                TimeSpan.FromMinutes(_options.IntervalMinutes),
                stoppingToken);
        }
    }
}