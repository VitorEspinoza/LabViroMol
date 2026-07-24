namespace LabViroMol.Modules.Research.Presentation.Partners;

using LabViroMol.Modules.Research.Application.Partners.Commands.Create;
using LabViroMol.Modules.Research.Application.Partners.Commands.Delete;
using LabViroMol.Modules.Research.Application.Partners.Commands.Update;
using LabViroMol.Modules.Research.Application.Partners.Queries;
using LabViroMol.Modules.Research.Application.Partners.ViewModels;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public record UpdatePartnerRequest(string Name, string? Description);

internal static class PartnerEndpoints
{
    public static void MapPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/partners").WithTags("Partners");

        group.MapPost("/", async (CreatePartnerCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        }).RequireAuthorization(Permissions.Research.PartnersManage);

        group.MapGet("/", async ([AsParameters] PagedRequest request, IPartnerQueries queries) =>
            Results.Ok(await queries.GetAllAdminAsync(request)))
            .Produces<PagedResponse<PartnerAdminSummaryViewModel>>(StatusCodes.Status200OK)
            .RequireAuthorization(Permissions.Research.PartnersView);

        group.MapGet("/{id:guid}", async (Guid id, IPartnerQueries queries) =>
        {
            var partner = await queries.GetById(id);
            return partner is null
                ? Results.NotFound()
                : Results.Ok(partner);
        }).Produces<PartnerViewModel>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status404NotFound)
          .RequireAuthorization(Permissions.Research.PartnersView);

        group.MapPut("/{id:guid}", async (Guid id, UpdatePartnerRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdatePartnerCommand(id, request.Name, request.Description);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PartnersManage);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeletePartnerCommand(id);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization(Permissions.Research.PartnersManage);
    }

    public static void MapInstitutionalPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/partners").WithTags("Partners-Public");

        group.MapGet("/", async ([AsParameters] PagedRequest request, IPartnerQueries queries) =>
            Results.Ok(await queries.GetAllInstitutionalAsync(request)))
            .Produces<PagedResponse<PartnerSummaryViewModel>>(StatusCodes.Status200OK);
    }
}
