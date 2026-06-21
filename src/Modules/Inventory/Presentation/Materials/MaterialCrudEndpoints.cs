using LabViroMol.Modules.Inventory.Application.Materials.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Materials.Commands.Update;
using LabViroMol.Modules.Inventory.Application.Materials.Queries;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Unit = LabViroMol.Modules.Inventory.Domain.Materials.Unit;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Inventory.Presentation.Materials;

public record CreateMaterialRequest(string Name, string Location, decimal MinStock, decimal StockQuantity, Unit Unit, Guid TypeId);
public record UpdateMaterialRequest(string Name, string Location, decimal MinStock);

internal static class MaterialCrudEndpoints
{
    public static void MapMaterialCrudEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/materials").WithTags("Materials");

        group.MapPost("/", async (CreateMaterialRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateMaterialCommand(
                request.Name,
                request.Location,
                (Quantity)request.MinStock,
                (Quantity)request.StockQuantity,
                request.Unit,
                MaterialTypeId.From(request.TypeId));
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Created());
        }).Accepts<CreateMaterialRequest>("application/json")
          .RequireAuthorization(Permissions.Inventory.MaterialsManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, IMaterialQueries queries) =>
            Results.Ok(await queries.GetAllAsync(request)))
            .RequireAuthorization(Permissions.Inventory.MaterialsView);

        group.MapGet("/{id:guid}", async (Guid id, IMaterialQueries queries) =>
        {
            var material = await queries.GetById(id);

            return material is null
                ? Results.NotFound()
                : Results.Ok(material);
        }).RequireAuthorization(Permissions.Inventory.MaterialsView);

        group.MapPut("/{id:guid}", async (Guid id, UpdateMaterialRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateMaterialCommand(MaterialId.From(id), request.Name, request.Location, (Quantity)request.MinStock);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.NoContent());
        }).Accepts<UpdateMaterialRequest>("application/json")
          .RequireAuthorization(Permissions.Inventory.MaterialsManage);
    }
}
