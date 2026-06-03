using System;
using System.Threading;
using LabViroMol.Modules.Inventory.Application.Materials.Commands.AddStock;
using LabViroMol.Modules.Inventory.Application.Materials.Commands.AddStockException;
using LabViroMol.Modules.Inventory.Application.Materials.Commands.ConsumeForProject;
using LabViroMol.Modules.Inventory.Application.Materials.Commands.RemoveStockException;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Inventory.Presentation.Materials;

public record AddStockMaterialRequest(decimal Quantity, string? Reason);

public record WriteOffRequest(decimal Quantity, Guid? ProjectId, string? Reason);

internal static class MaterialStockEndpoints
{
    public static void MapMaterialStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/materials").WithTags("Materials");

        group.MapPost("/{id:guid}/add-stock", async (Guid id, AddStockMaterialRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new AddStockMaterialExceptionCommand(MaterialId.From(id), (Quantity)request.Quantity, request.Reason);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.NoContent());
        }).Accepts<AddStockMaterialRequest>("application/json");

        group.MapPost("/{id:guid}/write-off", async (Guid id, WriteOffRequest request, IMediator mediator, CancellationToken ct) =>
        {
            Result result;

            var hasProjectAssociated = request.ProjectId.HasValue && request.ProjectId.Value != Guid.Empty;
            if (hasProjectAssociated)
            {
                var command = new ConsumeMaterialForProjectCommand(
                    MaterialId.From(id),
                    (Quantity)request.Quantity,
                    ProjectId.From(request.ProjectId!.Value));

                result = await mediator.Send(command, ct);
            }
            else
            {
                var command = new RemoveStockMaterialExceptionCommand(
                    MaterialId.From(id),
                    (Quantity)request.Quantity,
                    request.Reason ?? string.Empty);

                result = await mediator.Send(command, ct);
            }

            return result.ToHttpResult(Results.NoContent());
        }).Accepts<WriteOffRequest>("application/json");
    }

}
