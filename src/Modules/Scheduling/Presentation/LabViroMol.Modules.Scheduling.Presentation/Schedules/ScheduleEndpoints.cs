using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Schedules;
using LabViroMol.Modules.Shared.Presentation.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace LabViroMol.Modules.Scheduling.Presentation.Schedules;

internal static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/schedules").WithTags("Schedules");
        
        group.MapPost("/", async (CreateScheduleCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (ScheduleQueries scheduleQueries, CancellationToken ct) => Results.Ok(await scheduleQueries.GetAllSchedules()));

        group.MapGet("/pending",
            async (ScheduleQueries scheduleQueries) => Results.Ok(await scheduleQueries.GetAllSchedulesPending()));

        group.MapPatch("/{id:guid}/approve",
            async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var command = new ApproveScheduleCommand(ScheduleId.From(id));
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult(Results.Accepted());
            });
    }
}