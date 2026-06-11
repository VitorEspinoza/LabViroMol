using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Shared.Infrastructure.Translation;

public class TranslationBackgroundWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TranslationBackgroundWorker> _logger;

    public TranslationBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TranslationBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                TimeSpan.FromMinutes(5),
                stoppingToken);
        }
    }
}