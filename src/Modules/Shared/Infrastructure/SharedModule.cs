using LabViroMol.Modules.Shared.Infrastructure.Exceptions;
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
        services.AddHttpClient<ITextTranslator,
            LibreTranslator>(client =>
        {
            client.BaseAddress =
                new Uri("http://localhost:5000");
        });
        return services;
    }
}