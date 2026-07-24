using LabViroMol.Modules.Scheduling.Application;
using LabViroMol.Modules.Scheduling.Infrastructure;
using LabViroMol.Modules.Scheduling.Presentation.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Scheduling.Presentation;

public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling")
            .WithTags("Scheduling");

        group.MapScheduleEndpoints();

        var publicGroup = group.MapGroup("/public")
            .WithTags("Assets-Public")
            .WithMetadata(new AllowAnonymousAttribute());
        publicGroup.MapInstitutionalScheduleEndpoints();

        return app;
    }
}
