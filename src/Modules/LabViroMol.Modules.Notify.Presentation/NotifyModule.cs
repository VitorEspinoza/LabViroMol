using LabViroMol.Modules.Notify.Application;
using LabViroMol.Modules.Notify.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Notify.Presentation;

public static class NotifyModule
{
    public static IServiceCollection AddNotifyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);
        
        return services;
    }
}