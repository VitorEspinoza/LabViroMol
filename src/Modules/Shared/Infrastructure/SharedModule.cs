using LabViroMol.Modules.Shared.Infrastructure.Exceptions;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using LabViroMol.Modules.Shared.Infrastructure.Storage;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Shared.Infrastructure;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddHttpContextAccessor();
        services.AddSingleton<LabViroMolMetrics>();
        services.AddSingleton<EmailMetrics>();

        return services;
    }

    public static IServiceCollection AddStorages(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageSettings>(
            configuration.GetSection("Storage"));

        services.AddScoped<IFileStorage, LocalFileStorage>();

        return services;
    }

    public static IServiceCollection AddTranslator(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<TranslationOptions>(
            configuration.GetSection("Translation"));

        services.AddHostedService<TranslationBackgroundWorker>();

        if (configuration.GetValue("LoadTest:UseNoOpTranslator", false))
        {
            services.AddSingleton<ITextTranslator, NoOpTextTranslator>();
            return services;
        }

        services.AddHttpClient<ITextTranslator, LibreTranslator>(client =>
        {
            client.BaseAddress = new Uri(configuration["Translation:BaseUrl"] ?? "http://libretranslate:5000");
        });
        return services;
    }
}
