using LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;
using LabViroMol.Modules.Assets.Infrastructure.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using LabViroMol.Modules.Shared.Presentation.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Assets.Presentation.MaintenanceRequests;

internal static class MaintenanceRequestsEndpoints
{
    public static void MapMaintenanceRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/maintenance-requests").WithTags("MaintenanceRequests");

        group.MapPost("/", async (CreateMaintenanceCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.Created());
        });

        group.MapGet("/", async (MaintenanceRequestQueries maintenanceRequestQueries) =>
            Results.Ok(await maintenanceRequestQueries.GetAllMaintenanceRequestsAsync()));
    }
}