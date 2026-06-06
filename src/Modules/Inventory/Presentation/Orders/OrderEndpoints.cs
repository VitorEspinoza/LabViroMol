using LabViroMol.Modules.Inventory.Application.Orders.Commands.Create;
using LabViroMol.Modules.Inventory.Application.Orders.Commands.FixDetails;
using LabViroMol.Modules.Inventory.Application.Orders.Commands.Process;
using LabViroMol.Modules.Inventory.Application.Orders.Commands.Cancel;
using LabViroMol.Modules.Inventory.Application.Orders.Commands.Receive;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Inventory.Infrastructure.Orders;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Inventory.Presentation.Orders;

public record CreateOrderRequest(Guid MaterialId, Guid ProjectId, decimal Quantity, string Description);
public record FixOrderDetailsRequest(Guid NewProjectId, decimal NewQuantity, string Description);
public record ProcessOrderRequest(string? Notes);
public record ReceiveOrderRequest(decimal QuantityReceived, string? Notes);

internal static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/", async (CreateOrderRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateOrderCommand(
                MaterialId.From(request.MaterialId),
                ProjectId.From(request.ProjectId),
                (Quantity)request.Quantity,
                request.Description);

            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Created());
        }).RequireAuthorization(Permissions.Inventory.OrdersManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, OrderQueries queries) =>
            Results.Ok(await queries.GetAllAsync(request)))
            .RequireAuthorization(Permissions.Inventory.OrdersView);

        group.MapGet("/{id:guid}", async (Guid id, OrderQueries queries) =>
        {
            var order = await queries.GetById(id);

            return order is null
                ? Results.NotFound()
                : Results.Ok(order);
        }).RequireAuthorization(Permissions.Inventory.OrdersView);

        group.MapPut("/{id:guid}/fix-details", async (Guid id, FixOrderDetailsRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new FixOrderDetailsCommand(
                OrderId.From(id),
                ProjectId.From(request.NewProjectId),
                (Quantity)request.NewQuantity,
                request.Description);

            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.NoContent());
        }).Accepts<FixOrderDetailsRequest>("application/json")
          .RequireAuthorization(Permissions.Inventory.OrdersManage);

        group.MapPost("/{id:guid}/process", async (Guid id, ProcessOrderRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ProcessOrderCommand(OrderId.From(id), request.Notes);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).Accepts<ProcessOrderRequest>("application/json")
          .RequireAuthorization(Permissions.Inventory.OrdersManage);

        group.MapPost("/{id:guid}/receive", async (Guid id, ReceiveOrderRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ReceiveOrderCommand(
                OrderId.From(id),
                (Quantity)request.QuantityReceived,
                request.Notes);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).Accepts<ReceiveOrderRequest>("application/json")
          .RequireAuthorization(Permissions.Inventory.OrdersManage);

        group.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CancelOrderCommand(OrderId.From(id));
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Inventory.OrdersManage);
    }
}
