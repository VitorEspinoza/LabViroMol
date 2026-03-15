using LabViroMol.Modules.Inventory.Application.Kits.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Shared;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Update;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Kits;
using LabViroMol.Modules.Shared.Presentation.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Inventory.Presentation.Kits;

public record UpdateKitRequest(string Name, string Description, List<KitItemInputModel> Materials);

internal static class KitEndpoints
{
    public static void MapKitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kits").WithTags("Kits");

        group.MapPost("/", async (CreateKitCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (KitQueries kitQueries, CancellationToken ct) => Results.Ok(await kitQueries.GetAllKits()));

        group.MapGet("/{id:guid}", async (Guid id, KitQueries kitQueries) =>
        {
            var kit = await kitQueries.GetKitById(id);

            return kit is null
                ? Results.NotFound()
                : Results.Ok(kit);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateKitRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateKitCommand(KitId.From(id), request.Name, request.Description, request.Materials);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.NoContent());
        }).Accepts<UpdateKitRequest>("application/json");
    }
}
