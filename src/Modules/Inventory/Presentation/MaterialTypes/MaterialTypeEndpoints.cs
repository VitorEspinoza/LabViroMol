using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Activate;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Deactivate;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Create;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Queries;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.ViewModels;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Inventory.Presentation.MaterialTypes;

internal static class MaterialTypeEndpoints
{
    public static void MapMaterialTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/types").WithTags("MaterialTypes");

        group.MapPost("/", async (CreateMaterialTypeCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Created());
        }).RequireAuthorization(Permissions.Inventory.MaterialsManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, IMaterialTypeQueries queries) =>
            Results.Ok(await queries.GetAllAsync(request)))
            .Produces<PagedResponse<MaterialTypeViewModel>>(StatusCodes.Status200OK)
            .RequireAuthorization(Permissions.Inventory.MaterialsView);

        group.MapGet("/{id:guid}", async (Guid id, IMaterialTypeQueries queries) =>
        {
            var type = await queries.GetById(id);

            return type is null
                ? Results.NotFound()
                : Results.Ok(type);
        }).Produces<MaterialTypeViewModel>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status404NotFound)
          .RequireAuthorization(Permissions.Inventory.MaterialsView);

        group.MapPost("/{id:guid}/activate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ActivateMaterialTypeCommand(MaterialTypeId.From(id)), ct);

            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Inventory.MaterialsManage);

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeactivateMaterialTypeCommand(MaterialTypeId.From(id)), ct);

            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Inventory.MaterialsManage);
    }
}
