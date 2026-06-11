using GTranslate.Translators;
using LabViroMol.Modules.Shared.Infrastructure.Exceptions;
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
        services.AddHostedService<TranslationBackgroundWorker>();
        return services;
    }
}