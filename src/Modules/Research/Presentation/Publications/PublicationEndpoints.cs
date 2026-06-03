using System;
using System.Collections.Generic;
using System.Threading;

namespace LabViroMol.Modules.Research.Presentation.Publications;

using LabViroMol.Modules.Research.Application.Publications.Commands.AddResearcher;
using LabViroMol.Modules.Research.Application.Publications.Commands.AssignDoi;
using LabViroMol.Modules.Research.Application.Publications.Commands.Create;
using LabViroMol.Modules.Research.Application.Publications.Commands.Delete;
using LabViroMol.Modules.Research.Application.Publications.Commands.RemoveResearcher;
using LabViroMol.Modules.Research.Application.Publications.Commands.ReorderResearchers;
using LabViroMol.Modules.Research.Application.Publications.Commands.Update;
using LabViroMol.Modules.Research.Infrastructure.Publications;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public record CreatePublicationRequest(
    string Title,
    string Description,
    string Doi,
    DateOnly PublicationDate,
    string PublishedOn,
    string PublishUrl);

public record UpdatePublicationRequest(
    string Title,
    string Description,
    string PublishedOn,
    string PublishUrl);

public record AssignDoiRequest(string Doi);

public record AddPublicationResearcherRequest(Guid ResearcherId);

public record ReorderPublicationResearchersRequest(List<Guid> ResearcherIds);

internal static class PublicationEndpoints
{
    public static void MapPublicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/publications").WithTags("Publications");

        group.MapPost("/", async (CreatePublicationRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreatePublicationCommand(
                request.Title,
                request.Description,
                request.Doi,
                request.PublicationDate,
                request.PublishedOn,
                request.PublishUrl);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (PublicationQueries queries) =>
            Results.Ok(await queries.GetAll()));

        group.MapGet("/{id:guid}", async (Guid id, PublicationQueries queries) =>
        {
            var publication = await queries.GetById(id);
            return publication is null
                ? Results.NotFound()
                : Results.Ok(publication);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdatePublicationRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdatePublicationCommand(id, request.Title, request.Description, request.PublishedOn, request.PublishUrl);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPut("/{id:guid}/doi", async (Guid id, AssignDoiRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new AssignPublicationDoiCommand(id, request.Doi);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/researchers", async (Guid id, AddPublicationResearcherRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new AddPublicationResearcherCommand(id, request.ResearcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapDelete("/{id:guid}/researchers/{researcherId:guid}", async (Guid id, Guid researcherId, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RemovePublicationResearcherCommand(id, researcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPut("/{id:guid}/researchers/order", async (Guid id, ReorderPublicationResearchersRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ReorderPublicationResearchersCommand(id, request.ResearcherIds);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeletePublicationCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });
    }
}
