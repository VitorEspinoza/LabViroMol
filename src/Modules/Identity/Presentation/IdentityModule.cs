using LabViroMol.Modules.Identity.Application;
using LabViroMol.Modules.Identity.Application.Roles.CreateRole;
using LabViroMol.Modules.Identity.Application.Roles.DeleteRole;
using LabViroMol.Modules.Identity.Application.Roles.UpdateRolePermissions;
using LabViroMol.Modules.Identity.Infrastructure;
using LabViroMol.Modules.Identity.Infrastructure.Roles;
using LabViroMol.Modules.Identity.Presentation.Users;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Identity.Presentation;

public record UpdateRolePermissionsRequest(List<string> Permissions);

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
            Results.Ok(queries.GetAll()))
            .RequireAuthorization(Permissions.Identity.RolesView);

        group.MapGet("/roles", async (RoleQueries queries) =>
            Results.Ok(await queries.GetAllWithPermissions()))
            .RequireAuthorization(Permissions.Identity.RolesView);

        group.MapPost("/roles", async (CreateRoleCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        }).RequireAuthorization(Permissions.Identity.RolesManage);

        group.MapPut("/roles/{id:guid}/permissions", async (Guid id, UpdateRolePermissionsRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateRolePermissionsCommand(id, request.Permissions);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Ok());
        }).RequireAuthorization(Permissions.Identity.RolesManage)
          .Accepts<UpdateRolePermissionsRequest>("application/json");

        group.MapDelete("/roles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeleteRoleCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Identity.RolesManage);

        return app;
    }
}
