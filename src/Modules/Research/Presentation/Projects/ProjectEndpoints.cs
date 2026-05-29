namespace LabViroMol.Modules.Research.Presentation.Projects;

using LabViroMol.Modules.Research.Application.Projects.Commands.AddMember;
using LabViroMol.Modules.Research.Application.Projects.Commands.Cancel;
using LabViroMol.Modules.Research.Application.Projects.Commands.Complete;
using LabViroMol.Modules.Research.Application.Projects.Commands.Create;
using LabViroMol.Modules.Research.Application.Projects.Commands.RemoveMember;
using LabViroMol.Modules.Research.Application.Projects.Commands.ChangeMemberRole;
using LabViroMol.Modules.Research.Application.Projects.Commands.TransferLeadership;
using LabViroMol.Modules.Research.Application.Projects.Commands.Start;
using LabViroMol.Modules.Research.Application.Projects.Commands.Update;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Infrastructure.Projects;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public record CreateProjectRequest(Guid PrincipalInvestigatorId, string Title, string Description, Guid PartnerId);
public record UpdateProjectRequest(string Title, string Description, Guid RequestedById);
public record ResearcherIdRequest(Guid ResearcherId);
public record AddProjectMemberRequest(Guid ResearcherId, string Role, Guid RequestedById);
public record TransferLeadershipRequest(Guid NewLeadResearcherId, Guid RequestedById);
public record ChangeMemberRoleRequest(string NewRole, Guid RequestedById);

internal static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/projects").WithTags("Projects");

        group.MapPost("/", async (CreateProjectRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateProjectCommand(request.PrincipalInvestigatorId, request.Title, request.Description, request.PartnerId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (ProjectQueries queries) =>
            Results.Ok(await queries.GetAll()));

        group.MapGet("/{id:guid}", async (Guid id, ProjectQueries queries) =>
        {
            var project = await queries.GetById(id);
            return project is null
                ? Results.NotFound()
                : Results.Ok(project);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateProjectRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateProjectCommand(id, request.Title, request.Description, request.RequestedById);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/start", async (Guid id, ResearcherIdRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new StartProjectCommand(id, request.ResearcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/complete", async (Guid id, ResearcherIdRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CompleteProjectCommand(id, request.ResearcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ResearcherIdRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CancelProjectCommand(id, request.ResearcherId);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/members", async (Guid id, AddProjectMemberRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new AddProjectMemberCommand(id, request.ResearcherId, request.Role, request.RequestedById);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPost("/{id:guid}/transfer-leadership", async (Guid id, TransferLeadershipRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new TransferProjectLeadershipCommand(id, request.NewLeadResearcherId, request.RequestedById);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapPut("/{id:guid}/members/{researcherId:guid}/role", async (Guid id, Guid researcherId, ChangeMemberRoleRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ChangeProjectMemberRoleCommand(id, researcherId, request.NewRole, request.RequestedById);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });

        group.MapDelete("/{id:guid}/members/{researcherId:guid}", async (Guid id, Guid researcherId, Guid requestedById, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RemoveProjectMemberCommand(id, researcherId, requestedById);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        });
    }
}
