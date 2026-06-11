using System.Reflection;
using FluentValidation;
using LabViroMol.Modules.Assets.Application.Equipments.Jobs;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Assets.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ITranslationJob, EquipmentTranslationJob>();
        return services;
    }
}