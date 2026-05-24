using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Activate;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Deactivate;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Create;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.MaterialTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;

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
        });

        group.MapGet("/", async (MaterialTypeQueries queries) =>
            Results.Ok(await queries.GetAll()));

        group.MapGet("/{id:guid}", async (Guid id, MaterialTypeQueries queries) =>
        {
            var type = await queries.GetById(id);

            return type is null
                ? Results.NotFound()
                : Results.Ok(type);
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ActivateMaterialTypeCommand(MaterialTypeId.From(id)), ct);

            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeactivateMaterialTypeCommand(MaterialTypeId.From(id)), ct);

            return result.ToHttpResult(Results.NoContent());
        });
    }
}
