using LabViroMol.Modules.Research.Application;
using LabViroMol.Modules.Research.Infrastructure;
using LabViroMol.Modules.Research.Presentation.Partners;
using LabViroMol.Modules.Research.Presentation.Positions;
using LabViroMol.Modules.Research.Presentation.Projects;
using LabViroMol.Modules.Research.Presentation.Publications;
using LabViroMol.Modules.Research.Presentation.Researchers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.Presentation;

public static class ResearchModule
{
    public static IServiceCollection AddResearchModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddApplication()
            .AddInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapResearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/research")
            .WithTags("Research");

        group.MapPartnerEndpoints();
        group.MapProjectEndpoints();
        group.MapPositionEndpoints();
        group.MapPublicationEndpoints();

        var publicGroup = group.MapGroup("/public")
            .WithTags("Research-Public")
            .WithMetadata(new AllowAnonymousAttribute());
        publicGroup.MapInstitutionalPublicationEndpoints();
        publicGroup.MapInstitutionalPartnerEndpoints();
        publicGroup.MapInstitutionalProjectEndpoints();
        publicGroup.MapInstitutionalResearcherEndpoints();

        return app;
    }
}
