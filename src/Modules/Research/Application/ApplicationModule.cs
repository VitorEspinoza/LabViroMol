using LabViroMol.Modules.Research.Application.Projects.Integrations;
using LabViroMol.Modules.Research.Application.Researchers.Integrations;
using LabViroMol.Modules.Research.Contracts;

namespace LabViroMol.Modules.Research.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IProjectChecker, ProjectIntegrationService>();
        services.AddScoped<IResearcherProfileProvider, ResearcherProfileProvider>();
        return services;
    }
}
