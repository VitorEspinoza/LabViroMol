namespace LabViroMol.Modules.Research.Infrastructure;

using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Partners.Queries;
using LabViroMol.Modules.Research.Application.Positions.Queries;
using LabViroMol.Modules.Research.Application.Projects.Queries;
using LabViroMol.Modules.Research.Application.Publications.Queries;
using LabViroMol.Modules.Research.Application.Researchers.Queries;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Partners;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Positions;
using LabViroMol.Modules.Research.Infrastructure.Projects;
using LabViroMol.Modules.Research.Infrastructure.Publications;
using LabViroMol.Modules.Research.Infrastructure.Researchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddRepositories()
            .AddQueries()
            .AddContext(configuration);

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IResearcherRepository, ResearcherRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IPublicationRepository, PublicationRepository>();
        services.AddScoped<IResearchUnitOfWork, ResearchUnitOfWork>();
        services.AddScoped<IProjectChecker, ProjectIntegrationService>();
        services.AddScoped<IProjectCatalog, ProjectCatalog>();
        services.AddScoped<IResearcherProfileProvider, ResearcherProfileProvider>();
        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<IPartnerQueries, PartnerQueries>();
        services.AddScoped<IProjectQueries, ProjectQueries>();
        services.AddScoped<IResearcherQueries, ResearcherQueries>();
        services.AddScoped<IPositionQueries, PositionQueries>();
        services.AddScoped<IPublicationQueries, PublicationQueries>();
        return services;
    }

    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.ResolveLabViroMolConnectionString();

        services.AddDbContext<ResearchDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ResearchMigrationsHistory", "research");
                npgsqlOptions.MigrationsAssembly(typeof(ResearchDbContext).Assembly.FullName);
            }));

        services.AddOutbox<ResearchDbContext>();

        return services;
    }
}
