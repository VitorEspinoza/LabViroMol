using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public static class SlowQueryRegistrationExtensions
{
    public const string ConfigSectionName = "Observability";

    public static IServiceCollection AddSlowQueryLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SlowQueryOptions>()
            .Bind(configuration.GetSection(ConfigSectionName));

        services.TryAddSingleton<SlowQueryInterceptor>();

        return services;
    }
}
