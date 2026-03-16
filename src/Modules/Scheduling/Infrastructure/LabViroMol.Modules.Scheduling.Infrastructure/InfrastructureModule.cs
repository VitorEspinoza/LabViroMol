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
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services
                .AddRepositories()
                .AddContext(configuration);
        
            return services;
        }
        
        private IServiceCollection AddRepositories()
        {
            services.AddScoped<IScheduleRepository, ScheduleRepository>();
            services.AddScoped<ISchedulingUnitOfWork, SchedulingUnitOfWork>();
            return services;
        }
        
        private IServiceCollection AddContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("LabViroMol");
            
            services.AddDbContext<SchedulingDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__SchedulingMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(SchedulingDbContext).Assembly.FullName);
            
                }));
            
            return services;
        }
    }
}