namespace LabViroMol.Modules.Research.Presentation.Positions;

using LabViroMol.Modules.Research.Application.Positions.Commands.Create;
using LabViroMol.Modules.Research.Application.Positions.Commands.Delete;
using LabViroMol.Modules.Research.Infrastructure.Positions;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

internal static class PositionEndpoints
{
    public static void MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/positions").WithTags("Positions");

        group.MapPost("/", async (CreatePositionCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        }).RequireAuthorization(Permissions.Research.PositionsManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, PositionQueries queries) =>
            Results.Ok(await queries.GetAllAsync(request)))
            .RequireAuthorization(Permissions.Research.PositionsView);

        group.MapGet("/{id:guid}", async (Guid id, PositionQueries queries) =>
        {
            var position = await queries.GetById(id);
            return position is null
                ? Results.NotFound()
                : Results.Ok(position);
        }).RequireAuthorization(Permissions.Research.PositionsView);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeletePositionCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PositionsManage);
    }
}
