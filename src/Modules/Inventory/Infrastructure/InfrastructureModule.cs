using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.External;
using LabViroMol.Modules.Inventory.Infrastructure.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.Infrastructure;

public static class InfrastructureModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services.AddScoped<IProjectChecker, ProjectCheckerMock>();
            
            services
                .AddRepositories()
                .AddQueries()
                .AddContext(configuration);
        
            return services;
        }

        private IServiceCollection AddRepositories()
        {
            services.AddScoped<IKitRepository, KitRepository>();
            services.AddScoped<IMaterialRepository, MaterialRepository>();
            services.AddScoped<IMaterialTypeRepository, MaterialTypeRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IInventoryUnitOfWork, InventoryUnitOfWork>();
            services.AddScoped<MaterialValidatorService>();
            return services;
        }

        private IServiceCollection AddQueries()
        {
            services.AddScoped<KitQueries>();
            services.AddScoped<MaterialQueries>();
            services.AddScoped<MaterialTypeQueries>();
            services.AddScoped<OrderQueries>();
            return services;
        }

        private IServiceCollection AddContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("LabViroMol");
            
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__InventoryMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName);
            
                }));
            
            return services;
        }
    }
}