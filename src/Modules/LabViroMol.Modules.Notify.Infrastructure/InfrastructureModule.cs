using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Notify.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddRepositories()
            .AddContext(configuration);

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }
    
    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LabViroMol");

        services.AddDbContext<NotifyDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__NotifyMigrationsHistory");
                sqlOptions.MigrationsAssembly(typeof(NotifyDbContext).Assembly.FullName);
            }));

        return services;
    }
}