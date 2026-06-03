using System;
using System.Linq;
using System.Threading;
using LabViroMol.Modules.Notify.Application.Notifications.Commands.Dismiss;
using LabViroMol.Modules.Notify.Application.Notifications.Commands.DismissAll;
using LabViroMol.Modules.Notify.Application.Notifications.Commands.DismissBatch;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Notifications;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Notify.Presentation.Notifications;

internal static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").WithTags("Notifications");

        group.MapPost("/dismiss/all", async (DismissAllCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization();

        group.MapPost("/dismiss/batch", async (DismissBatchCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization();

        group.MapPost("/dismiss/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DismissCommand(NotificationId.From(id));
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(Results.NoContent());
        }).RequireAuthorization();

        group.MapGet("/", async (ICurrentUser currentUser, NotificationQueries queries) =>
        {
            var results = await queries.GetAllByPermissions(currentUser.Permissions.ToList());
            return results;
        }).RequireAuthorization();
            
    }
}