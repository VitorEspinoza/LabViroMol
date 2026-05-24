namespace LabViroMol.Modules.Research.Presentation.Researchers;

using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Researchers;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public record CreateResearcherRequest(
    string FirstName,
    string LastName,
    string Email,
    string LattesUrl,
    DegreeLevel DegreeLevel,
    string FieldOfStudy,
    Guid PositionId);

internal static class ResearcherEndpoints
{
    public static void MapResearcherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/researchers").WithTags("Researchers");
        
        group.MapGet("/", async (ResearcherQueries queries) =>
            Results.Ok(await queries.GetAll()));
        
    }
}
