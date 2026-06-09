using Microsoft.AspNetCore.Mvc;

namespace LabViroMol.Modules.Research.Presentation.Researchers;

using LabViroMol.Modules.Research.Infrastructure.Researchers;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

internal static class ResearcherEndpoints
{
    public static void MapResearcherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/researchers").WithTags("Researchers");

        group.MapGet("/", async ([FromQuery] string? language, [AsParameters] PagedRequest request, ResearcherQueries queries) =>
            Results.Ok(await queries.GetAllInstitutionalAsync(request, language)));
    }
}
