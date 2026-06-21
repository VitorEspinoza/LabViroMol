using LabViroMol.Modules.AdminBff.Application.Dashboard.Queries;
using LabViroMol.Modules.AdminBff.Infrastructure.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.AdminBff.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminDashboardQueries, AdminDashboardQueries>();
        return services;
    }
}
