using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Cancel;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Refuse;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.UploadTerm;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Schedules;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace LabViroMol.Modules.Scheduling.Presentation.Schedules;

public record ReproveScheduleRequest(string Justification);
public record CancelScheduleRequest(string Justification);

internal static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/schedules").WithTags("Schedules");

        group.MapGet("/", async ([AsParameters] PagedRequest request, ScheduleQueries scheduleQueries) =>
            Results.Ok(await scheduleQueries.GetAllAsync(request)))
            .RequireAuthorization(Permissions.Scheduling.SchedulesView);

        group.MapGet("/pending", async ([AsParameters] PagedRequest request, ScheduleQueries scheduleQueries) =>
            Results.Ok(await scheduleQueries.GetAllPendingAsync(request)))
            .RequireAuthorization(Permissions.Scheduling.SchedulesView);

        group.MapGet("/{id:guid}", async (Guid id, ScheduleQueries scheduleQueries) =>
        {
            var schedule = await scheduleQueries.GetByIdAsync(id);
            return schedule is null ? Results.NotFound() : Results.Ok(schedule);
        }).RequireAuthorization(Permissions.Scheduling.SchedulesView);

        group.MapPost("/{id:guid}/approve", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ApproveScheduleCommand(ScheduleId.From(id));
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Accepted());
        }).RequireAuthorization(Permissions.Scheduling.SchedulesManage);

        group.MapPost("/{id:guid}/refuse", async (Guid id, ReproveScheduleRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RefuseScheduleCommand(ScheduleId.From(id), request.Justification);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Accepted());
        }).RequireAuthorization(Permissions.Scheduling.SchedulesManage);
        
        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelScheduleRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CancelScheduleCommand(ScheduleId.From(id), request.Justification);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Accepted());
        }).RequireAuthorization(Permissions.Scheduling.SchedulesManage);

        group.MapPost("/{id:guid}/term", async (Guid id, IFormFile file, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UploadTermCommand(ScheduleId.From(id), file.OpenReadStream(), file.FileName);
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Accepted());
        }).DisableAntiforgery()
          .RequireAuthorization(Permissions.Scheduling.SchedulesManage);
    }

    public static void MapInstitutionalScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/schedules").WithTags("Schedules-Public");
        
        
        group.MapPost("/", async (CreateScheduleCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        }).RequireRateLimiting("SchedulingPolicy");
    }
}
