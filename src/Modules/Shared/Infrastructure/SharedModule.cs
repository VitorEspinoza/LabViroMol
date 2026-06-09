using GTranslate.Translators;
using LabViroMol.Modules.Shared.Infrastructure.Exceptions;
using LabViroMol.Modules.Shared.Infrastructure.Job;
using LabViroMol.Modules.Shared.Infrastructure.Storage;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Shared.Infrastructure;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddTranslator();
        services.AddJobs();
        
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

    private static IServiceCollection AddTranslator(
        this IServiceCollection services
    )
    {
        services.AddScoped<ITranslator, GoogleTranslator>();
        services.AddScoped<ITextTranslator, TextTranslator>();
        return services;
    }

    private static IServiceCollection AddJobs(
        this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();
        return services;
    }
}