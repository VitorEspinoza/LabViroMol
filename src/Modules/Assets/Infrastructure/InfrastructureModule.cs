using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Assets.Infrastructure.Storage;
using LabViroMol.Modules.Assets.Infrastructure.Storage.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Assets.Infrastructure;

public static class InfrastructureModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services
                .AddRepositories()
                .AddQueries()
                .AddStorages(configuration)
                .AddContext(configuration);
            
            return services;
        }

        private IServiceCollection AddQueries()
        {
            services.AddScoped<EquipmentQueries>();
            services.AddScoped<MaintenanceRequestQueries>();
            
            return services;
        }
        
        private IServiceCollection AddRepositories()
        {
            services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
            services.AddScoped<IAssetsUnitOfWork, AssetsUnitOfWork>();
            
            return services;
        }

        private IServiceCollection AddStorages(
            IConfiguration configuration)
        {
            services.Configure<StorageSettings>(
                configuration.GetSection("Storage"));

            services.AddScoped<
                IImageStorageService,
                LocalImageStorageService>();

            return services;
        }
        
        private IServiceCollection AddContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("LabViroMol");
            
            services.AddDbContext<AssetsDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__AssetsMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(AssetsDbContext).Assembly.FullName);
            
                }));
            
            return services;
        }
    }
}