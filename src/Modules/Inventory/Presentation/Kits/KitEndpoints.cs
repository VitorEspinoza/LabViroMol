using LabViroMol.Modules.Inventory.Application.Kits.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Shared;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Update;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Kits;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
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
        }).RequireAuthorization(Permissions.Inventory.KitsManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, KitQueries kitQueries) =>
            Results.Ok(await kitQueries.GetAllAsync(request)))
            .RequireAuthorization(Permissions.Inventory.KitsView);

        group.MapGet("/{id:guid}", async (Guid id, KitQueries kitQueries) =>
        {
            var kit = await kitQueries.GetKitById(id);

            return kit is null
                ? Results.NotFound()
                : Results.Ok(kit);
        }).RequireAuthorization(Permissions.Inventory.KitsView);

        group.MapPut("/{id:guid}", async (Guid id, UpdateKitRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateKitCommand(KitId.From(id), request.Name, request.Description, request.Materials);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.NoContent());
        }).Accepts<UpdateKitRequest>("application/json")
          .RequireAuthorization(Permissions.Inventory.KitsManage);
    }
}
