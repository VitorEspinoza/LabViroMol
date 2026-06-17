using LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Delete;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.UploadImage;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.Equipments;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Assets.Presentation.Equipments;

public record UpdateEquipmentRequest(string Name, string Brand, string Model, string Description, string? Location);

internal static class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/equipments").WithTags("Equipment");

        group.MapPost("/", async (CreateEquipmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        }).RequireAuthorization(Permissions.Assets.EquipmentsManage);

        group.MapPut("{id:guid}", async (Guid id, UpdateEquipmentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateEquipmentCommand(EquipmentId.From(id), request.Name, request.Model, request.Brand, request.Description, request.Location);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Accepted());
        }).Accepts<UpdateEquipmentRequest>("application/json")
          .RequireAuthorization(Permissions.Assets.EquipmentsManage);

        group.MapDelete("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeleteEquipmentCommand(EquipmentId.From(id));
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Assets.EquipmentsManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, EquipmentQueries equipmentQueries) =>
            Results.Ok(await equipmentQueries.GetAllAdminAsync(request)))
            .RequireAuthorization(Permissions.Assets.EquipmentsView);

        group.MapGet("{id:guid}", async (Guid id, EquipmentQueries equipmentQueries) =>
        {
            var equipment = await equipmentQueries.GetAdminByIdAsync(id);
            return equipment is null
                ? Results.NotFound()
                : Results.Ok(equipment);
        }).RequireAuthorization(Permissions.Assets.EquipmentsView);

        group.MapPost("{id:guid}/image", async (Guid id, IFormFile file, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new UploadImageCommand(
                    new EquipmentId(id),
                    file.OpenReadStream(),
                    file.FileName));

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest();
        }).DisableAntiforgery()
          .RequireAuthorization(Permissions.Assets.EquipmentsManage);
    }

    public static void MapInstitutionalEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/equipments").WithTags("Equipment-Public");

        group.MapGet("/", async ([AsParameters] PagedRequest request, [FromQuery] string? language, EquipmentQueries equipmentQueries) =>
            Results.Ok(await equipmentQueries.GetAllInstitutionalAsync(request, language)));

        group.MapGet("{id:guid}", async (Guid id, [FromQuery] string? language, EquipmentQueries equipmentQueries) =>
        {
            var equipment = await equipmentQueries.GetEquipmentByIdInstitutional(id, language);
            return equipment is null
                ? Results.NotFound()
                : Results.Ok(equipment);
        });
        
        group.MapGet("/schedulable", async ([FromQuery] string? language, EquipmentQueries equipmentQueries) =>
            Results.Ok(await equipmentQueries.GetSchedulableEquipments(language)));
    }
}
