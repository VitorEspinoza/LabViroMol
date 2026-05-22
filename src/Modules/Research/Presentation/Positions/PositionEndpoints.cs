namespace LabViroMol.Modules.Research.Presentation.Positions;

using LabViroMol.Modules.Research.Application.Positions.Commands.Create;
using LabViroMol.Modules.Research.Application.Positions.Commands.Delete;
using LabViroMol.Modules.Research.Infrastructure.Positions;
using LabViroMol.Modules.Shared.Presentation.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public record CreatePositionRequest(string Name, string Description);

internal static class PositionEndpoints
{
    public static void MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/positions").WithTags("Positions");

        group.MapPost("/", async (CreatePositionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreatePositionCommand(request.Name, request.Description);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (PositionQueries queries) =>
            Results.Ok(await queries.GetAll()));

        group.MapGet("/{id:guid}", async (Guid id, PositionQueries queries) =>
        {
            var position = await queries.GetById(id);
            return position is null
                ? Results.NotFound()
                : Results.Ok(position);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeletePositionCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });
    }
}
