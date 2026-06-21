using Microsoft.AspNetCore.Mvc;

namespace LabViroMol.Modules.Research.Presentation.Publications;

using LabViroMol.Modules.Research.Application.Publications.Commands.Create;
using LabViroMol.Modules.Research.Application.Publications.Commands.AssignDoi;
using LabViroMol.Modules.Research.Application.Publications.Commands.Delete;
using LabViroMol.Modules.Research.Application.Publications.Commands.AddResearcher;
using LabViroMol.Modules.Research.Application.Publications.Commands.RemoveResearcher;
using LabViroMol.Modules.Research.Application.Publications.Commands.ReorderResearchers;
using LabViroMol.Modules.Research.Application.Publications.Commands.Update;
using LabViroMol.Modules.Research.Application.Publications.Queries;
using LabViroMol.Modules.Research.Application.Publications.ViewModels;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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

        group.MapPost("/", async (CreatePublicationCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(id => Results.Created($"/api/research/publications/{id}", new { id }));
        }).RequireAuthorization(Permissions.Research.PublicationsManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, IPublicationQueries queries) =>
            Results.Ok(await queries.GetAllAdminAsync(request)))
            .RequireAuthorization(Permissions.Research.PublicationsView);

        group.MapGet("/{id:guid}", async (Guid id, IPublicationQueries queries) =>
        {
            var publication = await queries.GetById(id);
            return publication is null
                ? Results.NotFound()
                : Results.Ok(publication);
        }).RequireAuthorization(Permissions.Research.PublicationsView);

        group.MapPut("/{id:guid}", async (Guid id, UpdatePublicationRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdatePublicationCommand(id, request.Title, request.Description, request.PublishedOn, request.PublishUrl);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PublicationsManage);

        group.MapPut("/{id:guid}/doi", async (Guid id, AssignDoiRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new AssignPublicationDoiCommand(id, request.Doi);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PublicationsManage);

        group.MapPost("/{id:guid}/researchers", async (Guid id, AddPublicationResearcherRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new AddPublicationResearcherCommand(id, request.ResearcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PublicationsManage);

        group.MapDelete("/{id:guid}/researchers/{researcherId:guid}", async (Guid id, Guid researcherId, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RemovePublicationResearcherCommand(id, researcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PublicationsManage);

        group.MapPut("/{id:guid}/researchers/order", async (Guid id, ReorderPublicationResearchersRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ReorderPublicationResearchersCommand(id, request.ResearcherIds);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PublicationsManage);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeletePublicationCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PublicationsManage);
    }

    public static void MapInstitutionalPublicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/publications").WithTags("Publications-Public");

        group.MapGet("/", async ([FromQuery] string? language, [AsParameters] PagedRequest request, IPublicationQueries queries) =>
            Results.Ok(await queries.GetAllInstitutionalAsync(request, language)));
    }
}
