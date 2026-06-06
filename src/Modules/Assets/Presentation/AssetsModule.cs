using LabViroMol.Modules.Assets.Application;
using LabViroMol.Modules.Assets.Infrastructure;
using LabViroMol.Modules.Assets.Presentation.Equipments;
using LabViroMol.Modules.Assets.Presentation.MaintenanceRequests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Assets.Presentation;

public static class AssetsModule
{
    public static IServiceCollection AddAssetsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapAssetsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assets")
            .WithTags("Assets");

        group.MapEquipmentEndpoints();
        group.MapMaintenanceRequestsEndpoints();

        var publicGroup = group.MapGroup("/public").WithTags("Assets-Public");
        publicGroup.MapInstitutionalEquipmentEndpoints();

        return app;
    }
}
