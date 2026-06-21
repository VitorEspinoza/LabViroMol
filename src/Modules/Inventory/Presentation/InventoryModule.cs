using LabViroMol.Modules.Inventory.Application;
using LabViroMol.Modules.Inventory.Infrastructure;
using LabViroMol.Modules.Inventory.Presentation.Kits;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Inventory.Presentation.MaterialTypes;
using LabViroMol.Modules.Inventory.Presentation.Orders;
using LabViroMol.Modules.Inventory.Presentation.Reports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Inventory.Presentation;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .WithTags("Inventory");

        group.MapKitEndpoints();
        group.MapMaterialCrudEndpoints();
        group.MapMaterialStockEndpoints();
        group.MapMaterialTypeEndpoints();
        group.MapOrderEndpoints();
        group.MapInventoryReportEndpoints();

        return app;
    }
}
