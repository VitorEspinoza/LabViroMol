using System;
using System.Threading;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.UploadImage;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.Equipments;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
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
            async (EquipmentQueries equipmentQueries) =>
                Results.Ok(await equipmentQueries.GetAllEquipments()));

        group.MapGet("{id:guid}",
            async (Guid id, EquipmentQueries equipmentQueries) =>
            {
                var equipment = await equipmentQueries.GetEquipmentById(id);

                return equipment is null
                    ? Results.NotFound()
                    : Results.Ok(equipment);
            });
        
        group.MapPost("{id:guid}/image",
            async (
                Guid id,
                IFormFile file,
                IMediator mediator) =>
            {
                var result = await mediator.Send(
                    new UploadImageCommand(
                        EquipmentId.From(id),
                        file.OpenReadStream(),
                        file.FileName));

                return result.IsSuccess
                    ? Results.Ok()
                    : Results.BadRequest();
            })
            .DisableAntiforgery();
    }
}