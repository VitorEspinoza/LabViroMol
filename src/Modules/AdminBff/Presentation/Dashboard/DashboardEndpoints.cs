using LabViroMol.Modules.AdminBff.Application.Dashboard.Queries;
using LabViroMol.Modules.AdminBff.Application.Dashboard.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.AdminBff.Presentation.Dashboard;

internal static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard").WithTags("AdminBff-Dashboard");

        group.MapGet("/summary", async (
            IAdminDashboardQueries queries,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var summary = await queries.GetSummaryAsync(currentUser.Permissions, ct);

            return summary.Scheduling is null &&
                   summary.Inventory is null &&
                   summary.Assets is null
                ? Results.Forbid()
                : Results.Ok(summary);
        }).Produces<AdminDashboardSummaryViewModel>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status403Forbidden)
          .RequireAuthorization();
    }
}
