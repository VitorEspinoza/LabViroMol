using LabViroMol.Modules.AdminBff.Application;
using LabViroMol.Modules.AdminBff.Infrastructure;
using LabViroMol.Modules.AdminBff.Presentation.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.AdminBff.Presentation;

public static class AdminBffModule
{
    public static IServiceCollection AddAdminBffModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapAdminBffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("AdminBff");

        group.MapDashboardEndpoints();

        return app;
    }
}
