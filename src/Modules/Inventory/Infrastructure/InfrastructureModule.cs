using LabViroMol.Modules.Inventory.Application.Kits.Queries;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Queries;
using LabViroMol.Modules.Inventory.Application.Materials.Queries;
using LabViroMol.Modules.Inventory.Application.Orders.Queries;
using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Reports;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.Infrastructure;

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

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IKitRepository, KitRepository>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IMaterialTypeRepository, MaterialTypeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInventoryUnitOfWork, InventoryUnitOfWork>();
        services.AddScoped<MaterialValidatorService>();
        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<IKitQueries, KitQueries>();
        services.AddScoped<IMaterialQueries, MaterialQueries>();
        services.AddScoped<IMaterialTypeQueries, MaterialTypeQueries>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<IStockReportQueries, StockReportQueries>();
        services.AddScoped<IStockReportPdfGenerator, StockReportPdfGenerator>();
        services.AddScoped<IStockOutflowsReportQueries, StockOutflowsReportQueries>();
        services.AddScoped<IStockEntryBalanceAuditReportQueries, StockEntryBalanceAuditReportQueries>();
        return services;
    }

    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.ResolveLabViroMolConnectionString();

        services.AddSlowQueryLogging(configuration);

        services.AddDbContext<InventoryDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__InventoryMigrationsHistory", "inventory");
                npgsqlOptions.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName);
            })
            .AddInterceptors(serviceProvider.GetRequiredService<SlowQueryInterceptor>()));

        services.AddOutbox<InventoryDbContext>();

        return services;
    }
}
