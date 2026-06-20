using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public static class OutboxRegistrationExtensions
{
    public static IServiceCollection AddOutbox<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IOutboxProcessor, OutboxProcessor<TContext>>();
        return services;
    }

    public static IServiceCollection AddOutboxDispatcher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));
        services.AddSingleton<IPersistentEventTypeRegistry, PersistentEventTypeRegistry>();
        services.AddHostedService<OutboxBackgroundService>();
        return services;
    }
}
