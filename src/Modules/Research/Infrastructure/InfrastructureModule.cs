namespace LabViroMol.Modules.Research.Infrastructure;

using LabViroMol.Modules.Research.Application.Shared;
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
        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<PartnerQueries>();
        services.AddScoped<ProjectQueries>();
        services.AddScoped<ResearcherQueries>();
        services.AddScoped<PositionQueries>();
        services.AddScoped<PublicationQueries>();
        return services;
    }

    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LabViroMol");

        services.AddDbContext<ResearchDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__ResearchMigrationsHistory");
                sqlOptions.MigrationsAssembly(typeof(ResearchDbContext).Assembly.FullName);
            }));

        return services;
    }
}
