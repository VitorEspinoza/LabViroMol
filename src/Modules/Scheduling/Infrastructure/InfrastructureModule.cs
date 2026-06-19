using LabViroMol.Modules.Scheduling.Application.Schedules.Queries;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Infrastructure.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Scheduling.Infrastructure;

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

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<IScheduleQueries, ScheduleQueries>();
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ISchedulingUnitOfWork, SchedulingUnitOfWork>();
        return services;
    }

    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LabViroMol");

        services.AddDbContext<SchedulingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__SchedulingMigrationsHistory", "scheduling");
                npgsqlOptions.MigrationsAssembly(typeof(SchedulingDbContext).Assembly.FullName);
            }));

        return services;
    }
}
