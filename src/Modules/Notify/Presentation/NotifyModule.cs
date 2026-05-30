using LabViroMol.Modules.Notify.Application;
using LabViroMol.Modules.Notify.Infrastructure;
using LabViroMol.Modules.Notify.Presentation.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Notify.Presentation;

public static class NotifyModule
{
    public static IServiceCollection AddNotifyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);
        
        return services;
    }

    public static IEndpointRouteBuilder MapNotifyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notify")
            .WithTags("Notify");
        
        group.MapNotificationEndpoints();
        return app;
    }
}