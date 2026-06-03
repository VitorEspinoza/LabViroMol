using System;
using System.Threading;

namespace LabViroMol.Modules.Research.Presentation.Partners;

using LabViroMol.Modules.Research.Application.Partners.Commands.Create;
using LabViroMol.Modules.Research.Application.Partners.Commands.Delete;
using LabViroMol.Modules.Research.Application.Partners.Commands.Update;
using LabViroMol.Modules.Research.Infrastructure.Partners;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public record CreatePartnerRequest(string Name, string? Description);
public record UpdatePartnerRequest(string Name, string? Description);

internal static class PartnerEndpoints
{
    public static void MapPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/partners").WithTags("Partners");

        group.MapPost("/", async (CreatePartnerRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreatePartnerCommand(request.Name, request.Description);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (PartnerQueries queries) =>
            Results.Ok(await queries.GetAll()));

        group.MapGet("/{id:guid}", async (Guid id, PartnerQueries queries) =>
        {
            var partner = await queries.GetById(id);
            return partner is null
                ? Results.NotFound()
                : Results.Ok(partner);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdatePartnerRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdatePartnerCommand(id, request.Name, request.Description);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeletePartnerCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });
    }
}
