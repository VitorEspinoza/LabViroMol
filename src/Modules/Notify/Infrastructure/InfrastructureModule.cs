using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Emails;
using LabViroMol.Modules.Notify.Infrastructure.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
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
            .AddQueries()
            .AddContext(configuration);

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<INotificationRepository, NotificationRepository>()
            .AddScoped<INotifyUnitOfWork, NotifyUnitOfWork>();

        return services;
    }

    public static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services
            .AddScoped<INotificationQueries, NotificationQueries>();

        return services;
    }

    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LabViroMol");

        services.AddDbContext<NotifyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__NotifyMigrationsHistory", "notify");
                npgsqlOptions.MigrationsAssembly(typeof(NotifyDbContext).Assembly.FullName);
            }));

        services.AddOutbox<NotifyDbContext>();

        services.Configure<EmailOptions>(
            configuration.GetSection("Email"));

        services.AddScoped<ISendEmail, SmtpEmailSender>();

        return services;
    }
}
