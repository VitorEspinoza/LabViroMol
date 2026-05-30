using LabViroMol.Modules.Identity.Application;
using LabViroMol.Modules.Identity.Application.Roles.CreateRole;
using LabViroMol.Modules.Identity.Infrastructure;
using LabViroMol.Modules.Identity.Infrastructure.Roles;
using LabViroMol.Modules.Identity.Presentation.Users;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Identity.Presentation;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddIdentityInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity")
            .WithTags("Identity");

        group.MapUserEndpoints();

        group.MapGet("/permissions", (PermissionQueries queries) =>
            Results.Ok(queries.GetAll()));

        group.MapGet("/roles", async (RoleQueries queries) =>
            Results.Ok(await queries.GetAllWithPermissions()));

        group.MapPost("/roles", async (CreateRoleCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        return app;
    }
}
