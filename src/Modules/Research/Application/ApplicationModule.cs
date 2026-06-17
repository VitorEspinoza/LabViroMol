using LabViroMol.Modules.Research.Application.Positions.Jobs;
using LabViroMol.Modules.Research.Application.Projects.Jobs;
using LabViroMol.Modules.Research.Application.Publications.Jobs;
using LabViroMol.Modules.Shared.Infrastructure.Translation;

namespace LabViroMol.Modules.Research.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ITranslationJob, PositionTranslationJob>();
        services.AddScoped<ITranslationJob, ProjectTranslationJob>();
        services.AddScoped<ITranslationJob, PublicationTranslationJob>();
        return services;
    }
}
