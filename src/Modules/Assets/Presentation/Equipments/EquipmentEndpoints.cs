using LabViroMol.Modules.Assets.Application.Equipments.Command.Create;
using LabViroMol.Modules.Assets.Application.Equipments.Command.Update;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.Equipments;
using LabViroMol.Modules.Shared.Presentation.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Assets.Presentation.Equipments;

public record UpdateEquipmentRequest(string Name, string Brand, string Model, string Code, string Description);

internal static class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/equipments").WithTags("Equipment");

        group.MapPost("/", async (CreateEquipmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        group.MapPut("{id:guid}", async (Guid id, UpdateEquipmentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateEquipmentCommand(EquipmentId.From(id), request.Name, request.Model, request.Brand,  request.Code, request.Description);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Accepted());
        }).Accepts<UpdateEquipmentRequest>("application/json");

        group.MapGet("/",
            async (EquipmentQueries equipmentQueries, CancellationToken ct) =>
                Results.Ok(await equipmentQueries.GetAllEquipments()));
    }
}